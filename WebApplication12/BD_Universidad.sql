-- =============================================
-- Script de instalación — Sistema Académico
-- Ingeniería en Sistemas Informáticos UAI T109
-- Ejecutar completo en SQL Server Management Studio
-- =============================================

USE master
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BDUNIVERSIDAD')
    CREATE DATABASE BDUNIVERSIDAD
GO

USE BDUNIVERSIDAD
GO

-- =============================================
-- TABLAS
-- =============================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='USUARIO' AND xtype='U')
CREATE TABLE USUARIO (
    ID        INT IDENTITY(1,1) PRIMARY KEY,
    USUARIO   VARCHAR(50)    NOT NULL UNIQUE,
    PASS      VARCHAR(64)    NOT NULL,
    DIRECCION VARBINARY(256) NULL,
    ROL       VARCHAR(10)    NOT NULL DEFAULT 'Alumno'
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BITACORA' AND xtype='U')
CREATE TABLE BITACORA (
    ID      INT IDENTITY(1,1) PRIMARY KEY,
    USUARIO VARCHAR(50)  NOT NULL,
    ACCION  VARCHAR(100) NOT NULL,
    FECHA   DATETIME     NOT NULL DEFAULT GETDATE()
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MATERIA' AND xtype='U')
CREATE TABLE MATERIA (
    ID        INT IDENTITY(1,1) PRIMARY KEY,
    NOMBRE    VARCHAR(100) NOT NULL,
    CODIGO    VARCHAR(20)  NOT NULL UNIQUE,
    MODALIDAD VARCHAR(20)  NOT NULL DEFAULT 'Presencial',
    PESO      INT          NOT NULL DEFAULT 3,
    ACTIVA    BIT          NOT NULL DEFAULT 1
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CORRELATIVA' AND xtype='U')
CREATE TABLE CORRELATIVA (
    ID_MATERIA     INT NOT NULL REFERENCES MATERIA(ID),
    ID_CORRELATIVA INT NOT NULL REFERENCES MATERIA(ID),
    PRIMARY KEY (ID_MATERIA, ID_CORRELATIVA)
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ALUMNO_MATERIA' AND xtype='U')
CREATE TABLE ALUMNO_MATERIA (
    ID                  INT IDENTITY(1,1) PRIMARY KEY,
    ID_USUARIO          INT            NOT NULL REFERENCES USUARIO(ID),
    ID_MATERIA          INT            NOT NULL REFERENCES MATERIA(ID),
    ESTADO              VARCHAR(30)    NOT NULL DEFAULT 'Cursando',
    NOTA_PARCIAL1       DECIMAL(4,2)   NULL,
    NOTA_PARCIAL2       DECIMAL(4,2)   NULL,
    NOTA_RECUPERATORIO  DECIMAL(4,2)   NULL,
    NOTA_FINAL          DECIMAL(4,2)   NULL,
    FECHA_FINAL         DATETIME       NULL,
    FECHA_RECUPERATORIO DATETIME       NULL,
    DVH                 INT            NULL,
    UNIQUE(ID_USUARIO, ID_MATERIA)
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EVENTO_ACADEMICO' AND xtype='U')
CREATE TABLE EVENTO_ACADEMICO (
    ID          INT IDENTITY(1,1) PRIMARY KEY,
    ID_MATERIA  INT          NOT NULL REFERENCES MATERIA(ID),
    ID_USUARIO  INT          NOT NULL REFERENCES USUARIO(ID),
    TIPO        VARCHAR(30)  NOT NULL,
    DESCRIPCION VARCHAR(200) NULL,
    FECHA       DATETIME     NOT NULL,
    PESO        INT          NOT NULL DEFAULT 3
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DVV_NOTAS' AND xtype='U')
CREATE TABLE DVV_NOTAS (
    ID_USUARIO INT      NOT NULL REFERENCES USUARIO(ID) PRIMARY KEY,
    DVV        INT      NOT NULL,
    FECHA      DATETIME NOT NULL DEFAULT GETDATE()
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PERIODO_ACADEMICO' AND xtype='U')
CREATE TABLE PERIODO_ACADEMICO (
    ID           INT IDENTITY(1,1) PRIMARY KEY,
    ANIO         INT          NOT NULL,
    CUATRIMESTRE INT          NOT NULL,
    DESCRIPCION  VARCHAR(100) NULL,
    FECHA_INICIO DATE         NULL,
    FECHA_FIN    DATE         NULL,
    CONSTRAINT UQ_PERIODO UNIQUE (ANIO, CUATRIMESTRE)
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='INSCRIPCION' AND xtype='U')
CREATE TABLE INSCRIPCION (
    ID                INT IDENTITY(1,1) PRIMARY KEY,
    ID_USUARIO        INT      NOT NULL REFERENCES USUARIO(ID),
    ID_PERIODO        INT      NOT NULL REFERENCES PERIODO_ACADEMICO(ID),
    FECHA_INSCRIPCION DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_INSCRIPCION UNIQUE (ID_USUARIO, ID_PERIODO)
)
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='INSCRIPCION_DETALLE' AND xtype='U')
CREATE TABLE INSCRIPCION_DETALLE (
    ID             INT IDENTITY(1,1) PRIMARY KEY,
    ID_INSCRIPCION INT NOT NULL REFERENCES INSCRIPCION(ID),
    ID_MATERIA     INT NOT NULL REFERENCES MATERIA(ID)
)
GO

-- =============================================
-- STORED PROCEDURES — USUARIO
-- =============================================

IF OBJECT_ID('USUARIO_LOGIN','P') IS NOT NULL DROP PROCEDURE USUARIO_LOGIN
GO
CREATE PROCEDURE USUARIO_LOGIN
    @usuario VARCHAR(50),
    @pass    VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, ROL
    FROM   USUARIO
    WHERE  USUARIO = @usuario AND PASS = @pass;
END
GO

IF OBJECT_ID('USUARIO_OBTENER','P') IS NOT NULL DROP PROCEDURE USUARIO_OBTENER
GO
CREATE PROCEDURE USUARIO_OBTENER
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, DIRECCION, ROL
    FROM   USUARIO
    WHERE  USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_CAMBIAR_PASS','P') IS NOT NULL DROP PROCEDURE USUARIO_CAMBIAR_PASS
GO
CREATE PROCEDURE USUARIO_CAMBIAR_PASS
    @usuario VARCHAR(50),
    @pass    VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET PASS = @pass WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_ACTUALIZAR_DIRECCION','P') IS NOT NULL DROP PROCEDURE USUARIO_ACTUALIZAR_DIRECCION
GO
CREATE PROCEDURE USUARIO_ACTUALIZAR_DIRECCION
    @usuario   VARCHAR(50),
    @direccion VARBINARY(256)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET DIRECCION = @direccion WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_LISTAR_ALUMNOS','P') IS NOT NULL DROP PROCEDURE USUARIO_LISTAR_ALUMNOS
GO
CREATE PROCEDURE USUARIO_LISTAR_ALUMNOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO FROM USUARIO WHERE ROL = 'Alumno' ORDER BY USUARIO;
END
GO

-- =============================================
-- STORED PROCEDURES — BITACORA
-- =============================================

IF OBJECT_ID('BITACORA_INSERTAR','P') IS NOT NULL DROP PROCEDURE BITACORA_INSERTAR
GO
CREATE PROCEDURE BITACORA_INSERTAR
    @usuario VARCHAR(50),
    @accion  VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO BITACORA (USUARIO, ACCION, FECHA) VALUES (@usuario, @accion, GETDATE());
END
GO

IF OBJECT_ID('BITACORA_LISTAR','P') IS NOT NULL DROP PROCEDURE BITACORA_LISTAR
GO
CREATE PROCEDURE BITACORA_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, ACCION, FECHA FROM BITACORA ORDER BY FECHA DESC;
END
GO

-- =============================================
-- STORED PROCEDURES — MATERIA
-- =============================================

IF OBJECT_ID('MATERIA_INSERTAR','P') IS NOT NULL DROP PROCEDURE MATERIA_INSERTAR
GO
CREATE PROCEDURE MATERIA_INSERTAR
    @nombre    VARCHAR(100),
    @codigo    VARCHAR(20),
    @modalidad VARCHAR(20),
    @peso      INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO MATERIA (NOMBRE, CODIGO, MODALIDAD, PESO) VALUES (@nombre, @codigo, @modalidad, @peso);
    SELECT SCOPE_IDENTITY() AS ID;
END
GO

IF OBJECT_ID('MATERIA_ACTUALIZAR','P') IS NOT NULL DROP PROCEDURE MATERIA_ACTUALIZAR
GO
CREATE PROCEDURE MATERIA_ACTUALIZAR
    @id        INT,
    @nombre    VARCHAR(100),
    @codigo    VARCHAR(20),
    @modalidad VARCHAR(20),
    @peso      INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MATERIA SET NOMBRE=@nombre, CODIGO=@codigo, MODALIDAD=@modalidad, PESO=@peso WHERE ID=@id;
END
GO

IF OBJECT_ID('MATERIA_ELIMINAR','P') IS NOT NULL DROP PROCEDURE MATERIA_ELIMINAR
GO
CREATE PROCEDURE MATERIA_ELIMINAR
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MATERIA SET ACTIVA=0 WHERE ID=@id;
END
GO

IF OBJECT_ID('MATERIA_LISTAR','P') IS NOT NULL DROP PROCEDURE MATERIA_LISTAR
GO
CREATE PROCEDURE MATERIA_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, CODIGO, MODALIDAD, PESO FROM MATERIA WHERE ACTIVA=1;
END
GO

IF OBJECT_ID('MATERIA_OBTENER','P') IS NOT NULL DROP PROCEDURE MATERIA_OBTENER
GO
CREATE PROCEDURE MATERIA_OBTENER
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, CODIGO, MODALIDAD, PESO FROM MATERIA WHERE ID=@id AND ACTIVA=1;
END
GO

-- =============================================
-- STORED PROCEDURES — CORRELATIVAS
-- =============================================

IF OBJECT_ID('CORRELATIVA_INSERTAR','P') IS NOT NULL DROP PROCEDURE CORRELATIVA_INSERTAR
GO
CREATE PROCEDURE CORRELATIVA_INSERTAR
    @id_materia     INT,
    @id_correlativa INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM CORRELATIVA WHERE ID_MATERIA=@id_materia AND ID_CORRELATIVA=@id_correlativa)
        INSERT INTO CORRELATIVA VALUES (@id_materia, @id_correlativa);
END
GO

IF OBJECT_ID('CORRELATIVA_ELIMINAR','P') IS NOT NULL DROP PROCEDURE CORRELATIVA_ELIMINAR
GO
CREATE PROCEDURE CORRELATIVA_ELIMINAR
    @id_materia     INT,
    @id_correlativa INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM CORRELATIVA WHERE ID_MATERIA=@id_materia AND ID_CORRELATIVA=@id_correlativa;
END
GO

IF OBJECT_ID('CORRELATIVA_LISTAR','P') IS NOT NULL DROP PROCEDURE CORRELATIVA_LISTAR
GO
CREATE PROCEDURE CORRELATIVA_LISTAR
    @id_materia INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.ID, m.NOMBRE, m.CODIGO
    FROM   CORRELATIVA c
    JOIN   MATERIA m ON m.ID = c.ID_CORRELATIVA
    WHERE  c.ID_MATERIA = @id_materia AND m.ACTIVA = 1;
END
GO

-- =============================================
-- STORED PROCEDURES — ALUMNO_MATERIA
-- =============================================

IF OBJECT_ID('ALUMNO_MATERIA_INSERTAR','P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_INSERTAR
GO
CREATE PROCEDURE ALUMNO_MATERIA_INSERTAR
    @id_usuario INT,
    @id_materia INT,
    @estado     VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ALUMNO_MATERIA (ID_USUARIO, ID_MATERIA, ESTADO) VALUES (@id_usuario, @id_materia, @estado);
END
GO

IF OBJECT_ID('ALUMNO_MATERIA_ACTUALIZAR','P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_ACTUALIZAR
GO
CREATE PROCEDURE ALUMNO_MATERIA_ACTUALIZAR
    @id                  INT,
    @estado              VARCHAR(30),
    @nota_parcial1       DECIMAL(4,2),
    @nota_parcial2       DECIMAL(4,2),
    @nota_recuperatorio  DECIMAL(4,2),
    @nota_final          DECIMAL(4,2),
    @fecha_final         DATETIME,
    @fecha_recuperatorio DATETIME,
    @dvh                 INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ALUMNO_MATERIA
    SET    ESTADO              = @estado,
           NOTA_PARCIAL1       = @nota_parcial1,
           NOTA_PARCIAL2       = @nota_parcial2,
           NOTA_RECUPERATORIO  = @nota_recuperatorio,
           NOTA_FINAL          = @nota_final,
           FECHA_FINAL         = @fecha_final,
           FECHA_RECUPERATORIO = @fecha_recuperatorio,
           DVH                 = @dvh
    WHERE  ID = @id;
END
GO

IF OBJECT_ID('ALUMNO_MATERIA_LISTAR','P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_LISTAR
GO
CREATE PROCEDURE ALUMNO_MATERIA_LISTAR
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT am.ID, am.ID_MATERIA, m.NOMBRE, m.CODIGO, m.MODALIDAD, m.PESO,
           am.ESTADO,
           am.NOTA_PARCIAL1, am.NOTA_PARCIAL2,
           am.NOTA_RECUPERATORIO, am.NOTA_FINAL,
           am.FECHA_FINAL, am.FECHA_RECUPERATORIO,
           am.DVH
    FROM   ALUMNO_MATERIA am
    JOIN   MATERIA m ON m.ID = am.ID_MATERIA
    WHERE  am.ID_USUARIO = @id_usuario AND m.ACTIVA = 1;
END
GO

-- =============================================
-- STORED PROCEDURES — EVENTOS
-- =============================================

IF OBJECT_ID('EVENTO_INSERTAR','P') IS NOT NULL DROP PROCEDURE EVENTO_INSERTAR
GO
CREATE PROCEDURE EVENTO_INSERTAR
    @id_materia  INT,
    @id_usuario  INT,
    @tipo        VARCHAR(30),
    @descripcion VARCHAR(200),
    @fecha       DATETIME,
    @peso        INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO EVENTO_ACADEMICO (ID_MATERIA, ID_USUARIO, TIPO, DESCRIPCION, FECHA, PESO)
    VALUES (@id_materia, @id_usuario, @tipo, @descripcion, @fecha, @peso);
END
GO

IF OBJECT_ID('EVENTO_ACTUALIZAR','P') IS NOT NULL DROP PROCEDURE EVENTO_ACTUALIZAR
GO
CREATE PROCEDURE EVENTO_ACTUALIZAR
    @id          INT,
    @tipo        VARCHAR(30),
    @descripcion VARCHAR(200),
    @fecha       DATETIME,
    @peso        INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE EVENTO_ACADEMICO SET TIPO=@tipo, DESCRIPCION=@descripcion, FECHA=@fecha, PESO=@peso WHERE ID=@id;
END
GO

IF OBJECT_ID('EVENTO_ELIMINAR','P') IS NOT NULL DROP PROCEDURE EVENTO_ELIMINAR
GO
CREATE PROCEDURE EVENTO_ELIMINAR
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM EVENTO_ACADEMICO WHERE ID=@id;
END
GO

IF OBJECT_ID('EVENTO_LISTAR_USUARIO','P') IS NOT NULL DROP PROCEDURE EVENTO_LISTAR_USUARIO
GO
CREATE PROCEDURE EVENTO_LISTAR_USUARIO
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ID, e.ID_MATERIA, m.NOMBRE AS MATERIA, e.TIPO, e.DESCRIPCION, e.FECHA, e.PESO
    FROM   EVENTO_ACADEMICO e
    JOIN   MATERIA m ON m.ID = e.ID_MATERIA
    WHERE  e.ID_USUARIO = @id_usuario
    ORDER BY e.FECHA;
END
GO

IF OBJECT_ID('EVENTO_LISTAR_MES','P') IS NOT NULL DROP PROCEDURE EVENTO_LISTAR_MES
GO
CREATE PROCEDURE EVENTO_LISTAR_MES
    @id_usuario INT,
    @anio       INT,
    @mes        INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ID, e.ID_MATERIA, m.NOMBRE AS MATERIA, e.TIPO, e.DESCRIPCION, e.FECHA, e.PESO
    FROM   EVENTO_ACADEMICO e
    JOIN   MATERIA m ON m.ID = e.ID_MATERIA
    WHERE  e.ID_USUARIO = @id_usuario
      AND  YEAR(e.FECHA)  = @anio
      AND  MONTH(e.FECHA) = @mes
    ORDER BY e.FECHA;
END
GO

-- =============================================
-- STORED PROCEDURES — DVV
-- =============================================

IF OBJECT_ID('DVV_GUARDAR','P') IS NOT NULL DROP PROCEDURE DVV_GUARDAR
GO
CREATE PROCEDURE DVV_GUARDAR
    @id_usuario INT,
    @dvv        INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM DVV_NOTAS WHERE ID_USUARIO = @id_usuario)
        UPDATE DVV_NOTAS SET DVV=@dvv, FECHA=GETDATE() WHERE ID_USUARIO=@id_usuario;
    ELSE
        INSERT INTO DVV_NOTAS VALUES (@id_usuario, @dvv, GETDATE());
END
GO

IF OBJECT_ID('DVV_OBTENER','P') IS NOT NULL DROP PROCEDURE DVV_OBTENER
GO
CREATE PROCEDURE DVV_OBTENER
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DVV FROM DVV_NOTAS WHERE ID_USUARIO = @id_usuario;
END
GO

-- =============================================
-- STORED PROCEDURES — PERÍODO E INSCRIPCIÓN
-- =============================================

IF OBJECT_ID('PERIODO_LISTAR','P') IS NOT NULL DROP PROCEDURE PERIODO_LISTAR
GO
CREATE PROCEDURE PERIODO_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ANIO, CUATRIMESTRE, DESCRIPCION, FECHA_INICIO, FECHA_FIN
    FROM   PERIODO_ACADEMICO
    ORDER  BY ANIO DESC, CUATRIMESTRE DESC;
END
GO

IF OBJECT_ID('PERIODO_INSERTAR','P') IS NOT NULL DROP PROCEDURE PERIODO_INSERTAR
GO
CREATE PROCEDURE PERIODO_INSERTAR
    @anio         INT,
    @cuatrimestre INT,
    @descripcion  VARCHAR(100),
    @fecha_inicio DATE,
    @fecha_fin    DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PERIODO_ACADEMICO (ANIO, CUATRIMESTRE, DESCRIPCION, FECHA_INICIO, FECHA_FIN)
    VALUES (@anio, @cuatrimestre, @descripcion, @fecha_inicio, @fecha_fin);
    SELECT SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('PERIODO_ACTUALIZAR','P') IS NOT NULL DROP PROCEDURE PERIODO_ACTUALIZAR
GO
CREATE PROCEDURE PERIODO_ACTUALIZAR
    @id           INT,
    @anio         INT,
    @cuatrimestre INT,
    @descripcion  VARCHAR(100),
    @fecha_inicio DATE,
    @fecha_fin    DATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PERIODO_ACADEMICO
    SET    ANIO         = @anio,
           CUATRIMESTRE = @cuatrimestre,
           DESCRIPCION  = @descripcion,
           FECHA_INICIO = @fecha_inicio,
           FECHA_FIN    = @fecha_fin
    WHERE  ID = @id;
END
GO

IF OBJECT_ID('PERIODO_OBTENER_ACTUAL','P') IS NOT NULL DROP PROCEDURE PERIODO_OBTENER_ACTUAL
GO
CREATE PROCEDURE PERIODO_OBTENER_ACTUAL
    @hoy DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 ID, ANIO, CUATRIMESTRE, DESCRIPCION, FECHA_INICIO, FECHA_FIN
    FROM   PERIODO_ACADEMICO
    WHERE  @hoy BETWEEN FECHA_INICIO AND FECHA_FIN;
END
GO

IF OBJECT_ID('INSCRIPCION_INSERTAR','P') IS NOT NULL DROP PROCEDURE INSCRIPCION_INSERTAR
GO
CREATE PROCEDURE INSCRIPCION_INSERTAR
    @id_usuario INT,
    @id_periodo INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO INSCRIPCION (ID_USUARIO, ID_PERIODO) VALUES (@id_usuario, @id_periodo);
    SELECT SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('INSCRIPCION_DETALLE_INSERTAR','P') IS NOT NULL DROP PROCEDURE INSCRIPCION_DETALLE_INSERTAR
GO
CREATE PROCEDURE INSCRIPCION_DETALLE_INSERTAR
    @id_inscripcion INT,
    @id_materia     INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO INSCRIPCION_DETALLE (ID_INSCRIPCION, ID_MATERIA) VALUES (@id_inscripcion, @id_materia);
END
GO

IF OBJECT_ID('INSCRIPCION_EXISTE','P') IS NOT NULL DROP PROCEDURE INSCRIPCION_EXISTE
GO
CREATE PROCEDURE INSCRIPCION_EXISTE
    @id_usuario INT,
    @id_periodo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM INSCRIPCION
    WHERE  ID_USUARIO = @id_usuario AND ID_PERIODO = @id_periodo;
END
GO

IF OBJECT_ID('INSCRIPCION_OBTENER_ID','P') IS NOT NULL DROP PROCEDURE INSCRIPCION_OBTENER_ID
GO
CREATE PROCEDURE INSCRIPCION_OBTENER_ID
    @id_usuario INT,
    @id_periodo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL((SELECT ID FROM INSCRIPCION WHERE ID_USUARIO = @id_usuario AND ID_PERIODO = @id_periodo), 0);
END
GO

IF OBJECT_ID('INSCRIPCION_LISTAR_USUARIO','P') IS NOT NULL DROP PROCEDURE INSCRIPCION_LISTAR_USUARIO
GO
CREATE PROCEDURE INSCRIPCION_LISTAR_USUARIO
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        i.ID                                                              AS ID_INSCRIPCION,
        i.ID_PERIODO,
        i.FECHA_INSCRIPCION,
        CAST(p.ANIO AS VARCHAR) + ' — ' + CAST(p.CUATRIMESTRE AS VARCHAR)
            + '° Cuatrimestre'                                            AS ETIQUETA_PERIODO,
        d.ID_MATERIA,
        m.NOMBRE                                                          AS NOMBRE_MATERIA,
        m.CODIGO                                                          AS CODIGO_MATERIA
    FROM      INSCRIPCION i
    JOIN      PERIODO_ACADEMICO   p ON i.ID_PERIODO     = p.ID
    LEFT JOIN INSCRIPCION_DETALLE d ON i.ID             = d.ID_INSCRIPCION
    LEFT JOIN MATERIA             m ON d.ID_MATERIA     = m.ID
    WHERE     i.ID_USUARIO = @id_usuario
    ORDER BY  i.ID DESC, m.NOMBRE;
END
GO

-- =============================================
-- DATOS INICIALES — USUARIO ADMIN
-- Contraseña: Admin123
-- =============================================

IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE USUARIO = 'admin')
    INSERT INTO USUARIO (USUARIO, PASS, ROL)
    VALUES ('admin', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'Admin');
GO

-- =============================================
-- DATOS INICIALES — PLAN DE ESTUDIOS
-- Ingeniería en Sistemas Informáticos UAI T109
-- Peso: 1=32h  2=48h  3=64h  4=80h  5=200h
-- =============================================

IF NOT EXISTS (SELECT 1 FROM MATERIA WHERE CODIGO = '01')
    INSERT INTO MATERIA (NOMBRE, CODIGO, MODALIDAD, PESO) VALUES
    ('Introducción a los Algoritmos y la Programación', '01', 'Presencial', 4),
    ('Arquitectura de Computadoras I',                  '02', 'Presencial', 4),
    ('Álgebra y Geometría Analítica',                   '03', 'Presencial', 4),
    ('Problemática del Mundo Actual',                   '04', 'Presencial', 3),
    ('Inglés I',                                        '05', 'Presencial', 1),
    ('Arquitectura de Computadoras II',                 '06', 'Presencial', 4),
    ('Cálculo Infinitesimal I',                         '07', 'Presencial', 4),
    ('Historia de la Ciencia y de la Técnica',          '08', 'Presencial', 3),
    ('Inglés II',                                       '09', 'Presencial', 1),
    ('Programación y Estructuras de Datos',             '10', 'Presencial', 4),
    ('Programación Orientada a Objetos',                '11', 'Presencial', 4),
    ('Análisis y Diseño de Sistemas I',                 '12', 'Presencial', 4),
    ('Cálculo Infinitesimal II',                        '13', 'Presencial', 4),
    ('Inglés III',                                      '14', 'Presencial', 1),
    ('Sistemas Operativos',                             '15', 'Presencial', 4),
    ('Análisis de Procesos Administrativos',            '16', 'Presencial', 3),
    ('Análisis y Diseño de Sistemas II',                '17', 'Presencial', 4),
    ('Física I',                                        '18', 'Presencial', 4),
    ('Probabilidad y Estadística',                      '19', 'Presencial', 4),
    ('Inglés IV',                                       '20', 'Presencial', 1),
    ('Ingeniería de Requerimientos',                    '21', 'Presencial', 2),
    ('Desarrollo y Arquitectura de Software',           '22', 'Presencial', 4),
    ('Análisis y Diseño de Sistemas III',               '23', 'Presencial', 2),
    ('Base de Datos',                                   '24', 'Presencial', 4),
    ('Tecnología de las Comunicaciones I',              '25', 'Presencial', 4),
    ('Física II',                                       '26', 'Presencial', 3),
    ('Ingeniería de Software',                          '27', 'Presencial', 4),
    ('Experiencias Formativas Obligatorias (3er Año)',  '28', 'Presencial', 4),
    ('Tecnología de las Comunicaciones II',             '29', 'Presencial', 4),
    ('Matemática Discreta y Autómatas',                 '30', 'Presencial', 3),
    ('Bases de Datos Aplicada',                         '31', 'Presencial', 4),
    ('Trabajo de Diploma',                              '32', 'Presencial', 4),
    ('Metodologías Ágiles',                             '33', 'Presencial', 4),
    ('Organización y Gestión Empresaria',               '34', 'Presencial', 3),
    ('Bases de Datos Avanzada',                         '35', 'Presencial', 4),
    ('Desarrollo y Arquitecturas WEB',                  '36', 'Presencial', 4),
    ('Experiencias Formativas Obligatorias (4to Año)',  '37', 'Presencial', 2),
    ('Administración de Proyectos',                     '38', 'Presencial', 3),
    ('Planificación Estratégica',                       '39', 'Presencial', 4),
    ('Inteligencia Artificial',                         '40', 'Presencial', 3),
    ('Metodologías y Desarrollos WEB',                  '41', 'Presencial', 4),
    ('Electromagnetismo I',                             '42', 'Presencial', 3),
    ('Sistema de Hardware',                             '43', 'Presencial', 4),
    ('Modelización Numérica',                           '44', 'Presencial', 4),
    ('Electromagnetismo II',                            '45', 'Presencial', 3),
    ('Práctica Profesional Supervisada',                '46', 'Presencial', 5),
    ('Seminario de Trabajo Final de Ingeniería',        '47', 'Presencial', 4),
    ('Auditoría de Sistemas',                           '48', 'Presencial', 3),
    ('Redes y Teleprocesamiento',                       '49', 'Presencial', 4),
    ('Seguridad Informática',                           '50', 'Presencial', 3),
    ('Trabajo Final de Ingeniería',                     '51', 'Presencial', 4);
GO

IF NOT EXISTS (SELECT 1 FROM CORRELATIVA WHERE ID_MATERIA = (SELECT ID FROM MATERIA WHERE CODIGO='06'))
BEGIN
    INSERT INTO CORRELATIVA (ID_MATERIA, ID_CORRELATIVA) VALUES
    ((SELECT ID FROM MATERIA WHERE CODIGO='06'), (SELECT ID FROM MATERIA WHERE CODIGO='02')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='07'), (SELECT ID FROM MATERIA WHERE CODIGO='03')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='09'), (SELECT ID FROM MATERIA WHERE CODIGO='05')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='10'), (SELECT ID FROM MATERIA WHERE CODIGO='01')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='11'), (SELECT ID FROM MATERIA WHERE CODIGO='10')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='13'), (SELECT ID FROM MATERIA WHERE CODIGO='07')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='14'), (SELECT ID FROM MATERIA WHERE CODIGO='09')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='15'), (SELECT ID FROM MATERIA WHERE CODIGO='06')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='17'), (SELECT ID FROM MATERIA WHERE CODIGO='12')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='18'), (SELECT ID FROM MATERIA WHERE CODIGO='07')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='20'), (SELECT ID FROM MATERIA WHERE CODIGO='14')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='21'), (SELECT ID FROM MATERIA WHERE CODIGO='12')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='22'), (SELECT ID FROM MATERIA WHERE CODIGO='11')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='23'), (SELECT ID FROM MATERIA WHERE CODIGO='17')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='24'), (SELECT ID FROM MATERIA WHERE CODIGO='12')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='25'), (SELECT ID FROM MATERIA WHERE CODIGO='16')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='26'), (SELECT ID FROM MATERIA WHERE CODIGO='18')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='27'), (SELECT ID FROM MATERIA WHERE CODIGO='17')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='27'), (SELECT ID FROM MATERIA WHERE CODIGO='21')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='27'), (SELECT ID FROM MATERIA WHERE CODIGO='22')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='29'), (SELECT ID FROM MATERIA WHERE CODIGO='25')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='31'), (SELECT ID FROM MATERIA WHERE CODIGO='24')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='32'), (SELECT ID FROM MATERIA WHERE CODIGO='23')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='32'), (SELECT ID FROM MATERIA WHERE CODIGO='24')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='32'), (SELECT ID FROM MATERIA WHERE CODIGO='27')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='33'), (SELECT ID FROM MATERIA WHERE CODIGO='17')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='35'), (SELECT ID FROM MATERIA WHERE CODIGO='24')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='36'), (SELECT ID FROM MATERIA WHERE CODIGO='22')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='39'), (SELECT ID FROM MATERIA WHERE CODIGO='34')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='40'), (SELECT ID FROM MATERIA WHERE CODIGO='22')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='41'), (SELECT ID FROM MATERIA WHERE CODIGO='36')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='42'), (SELECT ID FROM MATERIA WHERE CODIGO='26')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='43'), (SELECT ID FROM MATERIA WHERE CODIGO='18')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='44'), (SELECT ID FROM MATERIA WHERE CODIGO='19')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='45'), (SELECT ID FROM MATERIA WHERE CODIGO='42')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='46'), (SELECT ID FROM MATERIA WHERE CODIGO='32')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='47'), (SELECT ID FROM MATERIA WHERE CODIGO='38')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='47'), (SELECT ID FROM MATERIA WHERE CODIGO='39')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='47'), (SELECT ID FROM MATERIA WHERE CODIGO='41')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='48'), (SELECT ID FROM MATERIA WHERE CODIGO='16')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='49'), (SELECT ID FROM MATERIA WHERE CODIGO='29')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='50'), (SELECT ID FROM MATERIA WHERE CODIGO='43')),
    ((SELECT ID FROM MATERIA WHERE CODIGO='51'), (SELECT ID FROM MATERIA WHERE CODIGO='47'));
END
GO

PRINT '✔ BDUNIVERSIDAD creada y configurada correctamente.';
PRINT 'Admin: usuario=admin | contraseña=Admin123';
GO
