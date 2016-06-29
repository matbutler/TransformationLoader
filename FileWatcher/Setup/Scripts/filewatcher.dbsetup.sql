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

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetFilePatterns' ) BEGIN
	DROP PROCEDURE GetFilePatterns
END

EXEC('CREATE PROCEDURE GetFilePatterns
AS
BEGIN
	SELECT [Id],[Filepattern] FROM [FileProcessConfig]
END')

IF EXISTS ( SELECT * FROM sys.objects WHERE type = 'P' AND name = 'FileProcessEnqueue' ) BEGIN
	DROP PROCEDURE FileProcessEnqueue
END

EXEC('CREATE PROCEDURE FileProcessEnqueue
	@Filepath		NVARCHAR(MAX),
	@FileConfigId	INT
AS
BEGIN
	INSERT INTO [FileProcessQueue] ([FileProcessConfig_Id], [Filepath], [Status], [AddedDate])
	SELECT
		@FileConfigId,
		@Filepath,
		1,
		GETDATE()
END')