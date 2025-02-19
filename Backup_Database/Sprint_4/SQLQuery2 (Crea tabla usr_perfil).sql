/*Para crear la tabla usr_profile */

CREATE TABLE [dbo].[usr_profile](
[id] [int] IDENTITY(1,1) NOT NULL,
[nombre] varchar NULL,
[email] varchar NULL,
[password] varchar NULL,
[createdAt] [date] NULL,
[updateAt] [date] NULL,
[fecha nacimeinto] [datetime] NULL,
[Cel] varchar NULL,
[Género] varchar NULL,
[Estado] varchar NULL,
[Foto_usuario] varbinary NULL,
[id_User] [int] NOT NULL,
CONSTRAINT [PK_usr_profile] PRIMARY KEY CLUSTERED
(
[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
)
GO

ALTER TABLE [dbo].[usr_profile] WITH CHECK ADD CONSTRAINT [FK_usr_profile_User] FOREIGN KEY([id_User])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[usr_profile] CHECK CONSTRAINT [FK_usr_profile_User]
GO
