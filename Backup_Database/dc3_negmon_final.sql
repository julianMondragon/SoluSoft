-- SCRIPT COMPLETO DC3 NEGMON (FINAL)

ALTER TABLE usr_profile
ADD NumeroCertificadosDisponibles INT DEFAULT 0,
    NumeroCertificadosUsados INT DEFAULT 0,
    FechaVigenciaPaquete DATE NULL;

CREATE TABLE AreaTematica (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Clave NVARCHAR(10) NOT NULL,
    Nombre NVARCHAR(200) NOT NULL,
    Activo BIT DEFAULT 1
);

CREATE TABLE Ocupacion (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Clave NVARCHAR(10) NOT NULL,
    Nombre NVARCHAR(200) NOT NULL,
    Activo BIT DEFAULT 1
);

CREATE TABLE Empresa (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    RFC NVARCHAR(20) NULL,
    Direccion NVARCHAR(300) NULL,
    CertificadorId INT NOT NULL,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE()
);

CREATE TABLE Curso (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(300) NOT NULL,
    DuracionHoras INT NOT NULL,
    AreaTematicaId INT NOT NULL,
    CertificadorId INT NOT NULL,
    Activo BIT DEFAULT 1,
    FechaCreacion DATETIME DEFAULT GETDATE()
);

CREATE TABLE Capacitador (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    NumeroRegistroSTPS NVARCHAR(100) NOT NULL,
    CertificadorId INT NOT NULL,
    Activo BIT DEFAULT 1,
    RutaFirmaBase NVARCHAR(500) NULL,
    FechaRegistroFirma DATETIME NULL
);

CREATE TABLE Trabajador (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    CURP NVARCHAR(20) NOT NULL,
    Puesto NVARCHAR(200) NULL,
    OcupacionId INT NULL,
    EmpresaId INT NOT NULL,
    Activo BIT DEFAULT 1
);

CREATE TABLE DC3 (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Folio NVARCHAR(50) UNIQUE,
    TrabajadorId INT NOT NULL,
    EmpresaId INT NOT NULL,
    CursoId INT NOT NULL,
    CapacitadorId INT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    CertificadorId INT NOT NULL,
    Estatus NVARCHAR(50) DEFAULT 'Borrador',
    FechaCreacion DATETIME DEFAULT GETDATE()
);

CREATE TABLE DC3Firma (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DC3Id INT NOT NULL,
    TipoFirma NVARCHAR(50) NOT NULL,
    NombreFirmante NVARCHAR(200) NOT NULL,
    RutaArchivo NVARCHAR(500) NULL,
    FechaFirma DATETIME DEFAULT GETDATE()
);

CREATE TABLE DC3Documento (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DC3Id INT NOT NULL,
    RutaPDF NVARCHAR(500) NULL,
    RutaQR NVARCHAR(500) NULL,
    UrlPublica NVARCHAR(500) NULL,
    FechaGeneracion DATETIME DEFAULT GETDATE()
);

ALTER TABLE Empresa
ADD CONSTRAINT FK_Empresa_User FOREIGN KEY (CertificadorId) REFERENCES [User](id);

ALTER TABLE Curso
ADD CONSTRAINT FK_Curso_AreaTematica FOREIGN KEY (AreaTematicaId) REFERENCES AreaTematica(Id);

ALTER TABLE Curso
ADD CONSTRAINT FK_Curso_User FOREIGN KEY (CertificadorId) REFERENCES [User](id);

ALTER TABLE Capacitador
ADD CONSTRAINT FK_Capacitador_User FOREIGN KEY (CertificadorId) REFERENCES [User](id);

ALTER TABLE Trabajador
ADD CONSTRAINT FK_Trabajador_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresa(Id);

ALTER TABLE Trabajador
ADD CONSTRAINT FK_Trabajador_Ocupacion FOREIGN KEY (OcupacionId) REFERENCES Ocupacion(Id);

ALTER TABLE DC3
ADD CONSTRAINT FK_DC3_Trabajador FOREIGN KEY (TrabajadorId) REFERENCES Trabajador(Id);

ALTER TABLE DC3
ADD CONSTRAINT FK_DC3_Empresa FOREIGN KEY (EmpresaId) REFERENCES Empresa(Id);

ALTER TABLE DC3
ADD CONSTRAINT FK_DC3_Curso FOREIGN KEY (CursoId) REFERENCES Curso(Id);

ALTER TABLE DC3
ADD CONSTRAINT FK_DC3_Capacitador FOREIGN KEY (CapacitadorId) REFERENCES Capacitador(Id);

ALTER TABLE DC3
ADD CONSTRAINT FK_DC3_User FOREIGN KEY (CertificadorId) REFERENCES [User](id);

ALTER TABLE DC3Firma
ADD CONSTRAINT FK_DC3Firma_DC3 FOREIGN KEY (DC3Id) REFERENCES DC3(Id);

ALTER TABLE DC3Documento
ADD CONSTRAINT FK_DC3Documento_DC3 FOREIGN KEY (DC3Id) REFERENCES DC3(Id);

CREATE INDEX IX_DC3_Certificador ON DC3(CertificadorId);
CREATE INDEX IX_DC3_Trabajador ON DC3(TrabajadorId);
CREATE INDEX IX_DC3_Curso ON DC3(CursoId);
CREATE INDEX IX_DC3_Empresa ON DC3(EmpresaId);

CREATE INDEX IX_Trabajador_Empresa ON Trabajador(EmpresaId);
CREATE INDEX IX_Trabajador_Ocupacion ON Trabajador(OcupacionId);

CREATE INDEX IX_Curso_Certificador ON Curso(CertificadorId);
CREATE INDEX IX_Empresa_Certificador ON Empresa(CertificadorId);
CREATE INDEX IX_Capacitador_Certificador ON Capacitador(CertificadorId);

CREATE INDEX IX_DC3Firma_DC3 ON DC3Firma(DC3Id);
CREATE INDEX IX_DC3Documento_DC3 ON DC3Documento(DC3Id);
