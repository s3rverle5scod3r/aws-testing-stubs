using Xunit;

namespace aws_testing_stubs.data_load_to_sns_from_rds.Tests
{
    public class PlaceholderTestClass
    {
        [Theory]
        [InlineData("s3rverle5scod3r")]
        public void Default_Test(string placeholder)
        {
            // Arrange

            // Act

            // Assert
            Assert.Equal("s3rverle5scod3r", placeholder);
        }
    }
}
