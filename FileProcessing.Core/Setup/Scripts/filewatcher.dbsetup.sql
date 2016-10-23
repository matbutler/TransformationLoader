DROP TABLE FileProcessConfig
DROP TABLE FileProcessQueue
DROP TABLE FileProcessAudit

IF NOT EXISTS ( SELECT * FROM sys.objects WHERE type = 'U' AND name = 'FileProcessConfig' ) BEGIN
	CREATE TABLE [dbo].[FileProcessConfig](
		[Id]				INT IDENTITY(1,1) NOT NULL,
		[Filepattern]		NVARCHAR(max) NOT NULL,
		[ProcessConfig]		NVARCHAR(max) NOT NULL,
		CONSTRAINT [PK_FileProcessConfig] PRIMARY KEY CLUSTERED 
		(
			[Id] ASC
		)
	)
END

TRUNCATE TABLE FileProcessConfig
INSERT INTO FileProcessConfig([Filepattern], [ProcessConfig]) SELECT '.*',''

IF NOT EXISTS ( SELECT * FROM sys.objects WHERE type = 'U' AND name = 'FileProcessQueue' ) BEGIN
	CREATE TABLE [dbo].[FileProcessQueue](
		[Id]					INT IDENTITY(1,1) NOT NULL,
		[FileProcessConfig_Id]	INT NOT NULL,
		[Filepath]				NVARCHAR(max) NOT NULL,
		[Status]				INT NOT NULL,
		[AddedDate]				DATETIME NOT NULL,
		CONSTRAINT [PK_FileProcessQueue] PRIMARY KEY CLUSTERED 
		(
			[Id] ASC
		)
	)
END

IF NOT EXISTS ( SELECT * FROM sys.objects WHERE type = 'U' AND name = 'FileProcessAudit' ) BEGIN
	CREATE TABLE [dbo].[FileProcessAudit](
		[Id]					INT IDENTITY(1,1) NOT NULL,
		[Filepath]				NVARCHAR(max) NOT NULL,
		[FileAction]			INT NOT NULL,
		[AddedDate]				DATETIME NOT NULL,
		CONSTRAINT [PK_FileProcessAudit] PRIMARY KEY CLUSTERED 
		(
			[Id] ASC
		)
	)
END

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetFilePatterns' ) BEGIN
	DROP PROCEDURE GetFilePatterns
END

EXEC('CREATE PROCEDURE GetFilePatterns
AS
BEGIN
	SELECT [Id],[Filepattern] FROM [FileProcessConfig]
END')

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'FileProcessAuditLog' ) BEGIN
	DROP PROCEDURE FileProcessAuditLog
END

EXEC('CREATE PROCEDURE FileProcessAuditLog
	@Filepath	NVARCHAR(MAX),
	@FileAction	INT
AS
BEGIN
	INSERT INTO [FileProcessAudit] ([Filepath], [FileAction], [AddedDate])
	SELECT
		@Filepath,
		@FileAction,
		GETDATE()
END')

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'FileProcessEnqueue' ) BEGIN
	DROP PROCEDURE FileProcessEnqueue
END

EXEC('CREATE PROCEDURE FileProcessEnqueue
	@Filepath		NVARCHAR(MAX),
	@FileConfigId	INT
AS
BEGIN
	DECLARE @Result INT

	BEGIN TRAN
	IF EXISTS(SELECT 1 FROM [FileProcessQueue] WITH(UPDLOCK) WHERE [Filepath] = @Filepath)
	BEGIN
		UPDATE [FileProcessQueue]
		SET 
			[Status] = 5
		WHERE
			[Status] = 4
			AND [Filepath] = @Filepath

		SELECT @Result = CASE WHEN @@ROWCOUNT = 1 THEN 2 ELSE 3 END
	END
	ELSE
	BEGIN
		INSERT INTO [FileProcessQueue] ([FileProcessConfig_Id], [Filepath], [Status], [AddedDate])
		SELECT
			@FileConfigId,
			@Filepath,
			1, -- New
			GETDATE()

		SELECT @Result = 1
	END
	COMMIT TRAN

	SELECT @Result
END')

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetNextFileToProcess' ) BEGIN
	DROP PROCEDURE GetNextFileToProcess
END

EXEC('CREATE PROCEDURE GetNextFileToProcess
AS
BEGIN
	DECLARE @ids TABLE (id INT);

	;WITH LatestFile AS (
		SELECT TOP 1 
			[Id],
			[Status]
		FROM
			[FileProcessQueue]
		WHERE
			[Status] = 1
	)
	UPDATE LatestFile
	SET
		[Status] = 2
	OUTPUT INSERTED.Id INTO @ids


	SELECT
		fq.[Id],
		[Filepath],
		[ProcessConfig]
	FROM
		[FileProcessQueue] AS fq
		JOIN [FileProcessConfig] AS fc ON fq.FileProcessConfig_Id=fc.Id
	WHERE
		EXISTS(SELECT 1 FROM @ids AS i WHERE i.id = fq.Id)
END')