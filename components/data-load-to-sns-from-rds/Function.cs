using Amazon.Lambda.Core;
using nuget_class_library.class_library.aws.lambda;
using nuget_class_library.class_library.aws.sns;
using nuget_class_library.class_library.data;
using nuget_class_library.class_library.data.core;
using nuget_class_library.class_library.data.enums;
using nuget_class_library.class_library.errorHandling;
using nuget_class_library.class_library.exception;
using nuget_class_library.class_library.sql;
using Newtonsoft.Json;
using System.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace aws_testing_stubs.data_load_to_sns_from_rds
{
    public class Function : LambdaBase
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="Function"/> class.
        /// Decodes Environment credentials ahead of time to accomodate warm starts for the lifespan of the container.
        /// </summary>
        public Function()
        {
            GetStoreRuntimeEnvironment();
            GetAndStoreFailureTopic();
        }     

        /// <summary>
        /// Reads from sql server rds table and writes found records to the relevant sns topic for load testing of component.
        /// </summary>
        /// <returns></returns>
        public void FunctionHandler()
        {
            var rdsConnection = GetSqlConnection();
            SqlProcedureHelper sqlProcedureHelper = new();
            ErrorHandlingHelper errorHandlingHelper = new();

            MotorCustomer? sqlDataRecord;
            var count = 0;
            foreach (var dataRow in rdsConnection.GetDataTableFromStoredProcedure(
                "sp-stub-get-records").AsEnumerable())
            {
                try
                {
                    var stubId = dataRow.Field<int>("stubId");

                    sqlDataRecord = new MotorCustomer(
                        dataRow.Field<string>("reference"),
                        dataRow.Field<string>("webReference"),
                        dataRow.Field<string>("email"),
                        dataRow.Field<string>("title"),
                        dataRow.Field<string>("firstName"),
                        dataRow.Field<string>("surname"),
                        new Address(
                            dataRow.Field<string>("houseNumber"),
                            dataRow.Field<string>("addressLine1"),
                            dataRow.Field<string>("addressLine2"),
                            dataRow.Field<string>("addressLine3"),
                            dataRow.Field<string>("addressLine4"),
                            dataRow.Field<string>("postcode")),
                        dataRow.Field<string>("phone"),
                        dataRow.Field<string>("brand"),
                        dataRow.Field<bool>("active"),
                        dataRow.Field<string>("paymentType"),
                        new Premium(
                            dataRow.Field<decimal>("totalSellingPrice"),
                            dataRow.Field<decimal>("nettPremium"),
                            dataRow.Field<decimal>("grossPremium"),
                            dataRow.Field<decimal>("outstandingBalance"),
                            dataRow.Field<string>("cardNumber")),
                        new Finance(
                            dataRow.Field<decimal>("deposit"),
                            dataRow.Field<decimal>("interest"),
                            dataRow.Field<decimal>("apr"),
                            dataRow.Field<int>("totalNumberOfInstallments"),
                            dataRow.Field<decimal>("monthlyInstallmentAmount"),
                            dataRow.Field<decimal>("totalInstallmentAmount"),
                            dataRow.Field<string>("financeProvider"),
                            dataRow.Field<string>("bankSortCode"),
                            dataRow.Field<string>("bankAccountNumber")),
                        new Vehicle(
                            dataRow.Field<string>("vehicleRegistration"),
                            dataRow.Field<string>("make"),
                            dataRow.Field<string>("model")));

                    logHelper.LogDebug($"Sending track event to sns topic.");

                    var messageId = SnsTopicHelper.AddMessageToTopicReturnId(
                        $"customer_ref_{sqlDataRecord.Reference}_{UnitHelper.DateTimeToIsoString(DateTime.Now)}",
                        JsonConvert.SerializeObject(sqlDataRecord),
                        GetAndStoreOutputSubmissionTopic("SUBMISSION_TOPIC_ARN"));

                    logHelper.LogInfo($"Sending record to sns topic complete.");

                    sqlProcedureHelper.UpdateStubbedSourceData(stubId, messageId, "sp-stub-update-record");
                }
                catch (NullLambdaEnvironmentVariableException nullLambdaEnvironmentVariableException) // LambdaBase Exception
                {
                    logHelper.LogError($"NullLambdaEnvironmentVariableException: {nullLambdaEnvironmentVariableException.Message}\nStack trace: {nullLambdaEnvironmentVariableException.StackTrace}");
                    errorHandlingHelper.NotifyAndRecordError(RuntimeEnvironment.ToString(), $"NullLambdaEnvironmentVariableException: {nullLambdaEnvironmentVariableException.Message}.", "data-load-to-sns-from-rds", JsonConvert.SerializeObject(dataRow, SerializerSettings), FailureTopicArn, "sp-insert-error-into-table", nullLambdaEnvironmentVariableException);
                    continue;
                }
                catch (MissingDataException missingDataException)
                {
                    logHelper.LogError($"MissingDataException: {missingDataException.Message}\nStack trace: {missingDataException.StackTrace}");
                    errorHandlingHelper.NotifyAndRecordError(RuntimeEnvironment.ToString(), $"MissingDataException: {missingDataException.Message}.", "data-load-to-sns-from-rds", JsonConvert.SerializeObject(dataRow, SerializerSettings), FailureTopicArn, "sp-insert-error-into-table", missingDataException);
                    continue;
                }
                catch (EnumParseNotFoundException enumParseNotFoundException)
                {
                    logHelper.LogError($"EnumParseNotFoundException: {enumParseNotFoundException.Message}\nStack trace: {enumParseNotFoundException.StackTrace}");
                    errorHandlingHelper.NotifyAndRecordError(RuntimeEnvironment.ToString(), $"EnumParseNotFoundException: {enumParseNotFoundException.Message}.", "data-load-to-sns-from-rds", JsonConvert.SerializeObject(dataRow, SerializerSettings), FailureTopicArn, "sp-insert-error-into-table", enumParseNotFoundException);
                    continue;
                }
                catch (SqlExecuteStoredProcedureException sqlExecuteStoredProcedureException)
                {
                    logHelper.LogError($"EnumParseNotFoundException: {sqlExecuteStoredProcedureException.Message}\nStack trace: {sqlExecuteStoredProcedureException.StackTrace}");
                    errorHandlingHelper.NotifyAndRecordError(RuntimeEnvironment.ToString(), $"SqlExecuteStoredProcedureException: {sqlExecuteStoredProcedureException.Message}.", "data-load-to-sns-from-rds", JsonConvert.SerializeObject(dataRow, SerializerSettings), FailureTopicArn, "sp-insert-error-into-table", sqlExecuteStoredProcedureException);
                    continue;
                }
                catch (Exception exception)
                {
                    logHelper.LogError($"Exception: Failed to deliver payload to submission topic: {exception.Message}\nStack trace: {exception.StackTrace}");
                    errorHandlingHelper.NotifyAndRecordError(RuntimeEnvironment.ToString(), $"Exception: Failed to deliver payload to submission topic.", "data-load-to-sns-from-rds", JsonConvert.SerializeObject(dataRow, SerializerSettings), FailureTopicArn, "sp-insert-error-into-table", exception);
                    continue;
                }
                count++;
            }
            logHelper.LogDebug($"Processed {count} records.");
        }
    }
}
