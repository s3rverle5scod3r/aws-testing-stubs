USE [db_env]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp-stub-update-record]
    @stubId INT,
    @messageId VARCHAR(100)
AS
UPDATE [dbo].[stub_record_table]
SET
    messageId = @messageId
WHERE
    stubId = @stubId
