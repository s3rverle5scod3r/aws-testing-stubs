USE [db_env]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp-stub-get-records]
AS
SELECT *
FROM [dbo].[stub_record_table]
WHERE messageId IS NULL