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
    DIRECCION VARBINARY(256) NULL
)
GO

-- ACTIVO: baja lógica de usuarios (no hay borrado físico por las FK a cursadas y
-- bitácora). Va acá, antes de los SP de USUARIO, porque USUARIO_LOGIN la lee.
-- Si la columna se agrega ahora, el orden de columnas de integridad de USUARIO
-- cambió: se descartan sus DVV y snapshot para que se regeneren.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'ACTIVO' AND Object_ID = Object_ID('USUARIO'))
BEGIN
    ALTER TABLE USUARIO ADD ACTIVO BIT NOT NULL DEFAULT 1;
    IF OBJECT_ID('DIGITO_VERIFICADOR_VERTICAL', 'U') IS NOT NULL
        DELETE FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = 'USUARIO';
    IF OBJECT_ID('SNAPSHOT_INTEGRIDAD', 'U') IS NOT NULL
        DELETE FROM SNAPSHOT_INTEGRIDAD WHERE TABLA = 'USUARIO';
END
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
    SELECT ID, USUARIO, ACTIVO
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
    SELECT ID, USUARIO, DIRECCION
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
    SELECT @@ROWCOUNT AS FILAS;
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
    SELECT @@ROWCOUNT AS FILAS;
END
GO

-- USUARIO_LISTAR_ALUMNOS ya no existe: los alumnos se determinan en la BLL
-- según sus permisos efectivos (ver UsuarioBLL.ListarAlumnos).
IF OBJECT_ID('USUARIO_LISTAR_ALUMNOS','P') IS NOT NULL DROP PROCEDURE USUARIO_LISTAR_ALUMNOS
GO

-- =============================================
-- STORED PROCEDURES — BITACORA
-- =============================================

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
    SELECT @@ROWCOUNT AS FILAS;
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
    SELECT @@ROWCOUNT AS FILAS;
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
        INSERT INTO CORRELATIVA (ID_MATERIA, ID_CORRELATIVA) VALUES (@id_materia, @id_correlativa);
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
    SELECT SCOPE_IDENTITY() AS ID;
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
    SELECT @@ROWCOUNT AS FILAS;
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
    SELECT SCOPE_IDENTITY() AS ID;
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
    SELECT @@ROWCOUNT AS FILAS;
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
    SELECT @@ROWCOUNT AS FILAS;
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
    SELECT @@ROWCOUNT AS FILAS;
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
-- DATOS INICIALES — USUARIOS ADMIN Y WEBMASTER
-- Se siembran en la sección PERFILES Y PERMISOS (al final), junto con
-- la asignación de su perfil.
-- =============================================

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
PRINT 'Los usuarios admin (Admin123) y webmaster (Webmaster123) los crea la aplicación en su primer arranque.';
GO

-- =============================================
-- ===== INTEGRIDAD: DVH/DVV generalizado =====
-- Bloque idempotente — seguro de re-ejecutar sobre una base ya inicializada
-- con el script anterior. Generaliza el mecanismo de dígitos verificadores
-- horizontal (DVH, por fila) y vertical (DVV, por columna) a las 9 tablas
-- de negocio, reemplazando el mecanismo de propósito único que solo cubría
-- ALUMNO_MATERIA vía DVV_NOTAS (tabla que se deja en la base sin usar, por
-- las dudas de que algo la lea, pero el código C# ya no la toca).
-- =============================================

USE BDUNIVERSIDAD
GO

-- ---------------------------------------------
-- Columna DVH en cada una de las 9 tablas de negocio
-- ---------------------------------------------

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('USUARIO'))
    ALTER TABLE USUARIO ADD DVH INT NOT NULL DEFAULT 0
GO

-- INTENTOS_FALLIDOS/BLOQUEADO se agregan acá (no en el bloque de "Bloqueo de
-- cuenta" más abajo) porque USUARIO_LISTAR_PARA_INTEGRIDAD, unas líneas más
-- abajo en este mismo bloque, ya hace SELECT de esas columnas — tienen que
-- existir en la tabla ANTES de que se compile ese stored procedure.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'INTENTOS_FALLIDOS' AND Object_ID = Object_ID('USUARIO'))
    ALTER TABLE USUARIO ADD INTENTOS_FALLIDOS INT NOT NULL DEFAULT 0
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'BLOQUEADO' AND Object_ID = Object_ID('USUARIO'))
    ALTER TABLE USUARIO ADD BLOQUEADO BIT NOT NULL DEFAULT 0
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('BITACORA'))
    ALTER TABLE BITACORA ADD DVH INT NOT NULL DEFAULT 0
GO

-- CRITICIDAD se agrega acá (no en el bloque de "Bitácora: filtros y
-- paginado" más abajo) por la misma razón que INTENTOS_FALLIDOS/BLOQUEADO:
-- BITACORA_LISTAR_PARA_INTEGRIDAD, unas líneas más abajo en este mismo
-- bloque, ya hace SELECT de esta columna.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'CRITICIDAD' AND Object_ID = Object_ID('BITACORA'))
    ALTER TABLE BITACORA ADD CRITICIDAD VARCHAR(10) NOT NULL DEFAULT 'Media'
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('MATERIA'))
    ALTER TABLE MATERIA ADD DVH INT NOT NULL DEFAULT 0
GO

-- CORRELATIVA es tabla de asociación (PK compuesta ID_MATERIA/ID_CORRELATIVA,
-- sin columna ID propia). Para reutilizar el mismo motor genérico de
-- integridad (que opera sobre un ID entero simple por fila) se le agrega
-- una columna ID IDENTITY de respaldo. SQL Server completa los valores de
-- forma secuencial para las filas ya existentes al agregar la columna.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'ID' AND Object_ID = Object_ID('CORRELATIVA'))
    ALTER TABLE CORRELATIVA ADD ID INT IDENTITY(1,1) NOT NULL
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('CORRELATIVA'))
    ALTER TABLE CORRELATIVA ADD DVH INT NOT NULL DEFAULT 0
GO

-- ALUMNO_MATERIA ya tenía DVH INT NULL (mecanismo viejo) — se pasa a NOT NULL DEFAULT 0
IF EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('ALUMNO_MATERIA') AND is_nullable = 1)
BEGIN
    UPDATE ALUMNO_MATERIA SET DVH = 0 WHERE DVH IS NULL
    ALTER TABLE ALUMNO_MATERIA ALTER COLUMN DVH INT NOT NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE name = 'DF_ALUMNO_MATERIA_DVH')
    ALTER TABLE ALUMNO_MATERIA ADD CONSTRAINT DF_ALUMNO_MATERIA_DVH DEFAULT 0 FOR DVH
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('EVENTO_ACADEMICO'))
    ALTER TABLE EVENTO_ACADEMICO ADD DVH INT NOT NULL DEFAULT 0
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('PERIODO_ACADEMICO'))
    ALTER TABLE PERIODO_ACADEMICO ADD DVH INT NOT NULL DEFAULT 0
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('INSCRIPCION'))
    ALTER TABLE INSCRIPCION ADD DVH INT NOT NULL DEFAULT 0
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DVH' AND Object_ID = Object_ID('INSCRIPCION_DETALLE'))
    ALTER TABLE INSCRIPCION_DETALLE ADD DVH INT NOT NULL DEFAULT 0
GO

-- ---------------------------------------------
-- Tabla genérica de dígitos verificadores verticales (uno por TABLA+COLUMNA)
-- Reemplaza a DVV_NOTAS (que quedaba atada a un único usuario, sin noción
-- de columna). DVV_NOTAS se deja en la base sin usar; no se migra su
-- contenido porque usaba una fórmula distinta (mod 10) que este mecanismo
-- reemplaza — los DVV nuevos se generan desde cero la primera vez que se
-- invoque IntegridadBLL.RecalcularAlumnoMateria() (o RecalcularTodasLasTablas()).
-- ---------------------------------------------

IF OBJECT_ID('DIGITO_VERIFICADOR_VERTICAL', 'U') IS NULL
CREATE TABLE DIGITO_VERIFICADOR_VERTICAL (
    TABLA   VARCHAR(50) NOT NULL,
    COLUMNA VARCHAR(50) NOT NULL,
    DVV     INT         NOT NULL DEFAULT 0,
    CONSTRAINT PK_DVV PRIMARY KEY (TABLA, COLUMNA)
)
GO

-- ---------------------------------------------
-- STORED PROCEDURES — DVV genéricos
-- ---------------------------------------------

IF OBJECT_ID('DVV_LISTAR', 'P') IS NOT NULL DROP PROCEDURE DVV_LISTAR
GO
CREATE PROCEDURE DVV_LISTAR
    @tabla VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COLUMNA, DVV FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = @tabla;
END
GO

IF OBJECT_ID('DVV_ACTUALIZAR', 'P') IS NOT NULL DROP PROCEDURE DVV_ACTUALIZAR
GO
CREATE PROCEDURE DVV_ACTUALIZAR
    @tabla   VARCHAR(50),
    @columna VARCHAR(50),
    @dvv     INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = @tabla AND COLUMNA = @columna)
        UPDATE DIGITO_VERIFICADOR_VERTICAL SET DVV = @dvv WHERE TABLA = @tabla AND COLUMNA = @columna;
    ELSE
        INSERT INTO DIGITO_VERIFICADOR_VERTICAL (TABLA, COLUMNA, DVV) VALUES (@tabla, @columna, @dvv);
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — USUARIO (integridad)
-- ---------------------------------------------

IF OBJECT_ID('USUARIO_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE USUARIO_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE USUARIO_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, PASS, DIRECCION, INTENTOS_FALLIDOS, BLOQUEADO, ACTIVO, DVH FROM USUARIO ORDER BY ID;
END
GO

IF OBJECT_ID('USUARIO_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE USUARIO_ACTUALIZAR_DVH
GO
CREATE PROCEDURE USUARIO_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — BITACORA (integridad)
-- ---------------------------------------------

IF OBJECT_ID('BITACORA_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE BITACORA_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE BITACORA_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, ACCION, FECHA, CRITICIDAD, DVH FROM BITACORA ORDER BY ID;
END
GO

IF OBJECT_ID('BITACORA_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE BITACORA_ACTUALIZAR_DVH
GO
CREATE PROCEDURE BITACORA_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE BITACORA SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — MATERIA (integridad)
-- ---------------------------------------------

IF OBJECT_ID('MATERIA_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE MATERIA_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE MATERIA_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, CODIGO, MODALIDAD, PESO, ACTIVA, DVH FROM MATERIA ORDER BY ID;
END
GO

IF OBJECT_ID('MATERIA_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE MATERIA_ACTUALIZAR_DVH
GO
CREATE PROCEDURE MATERIA_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MATERIA SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — CORRELATIVA (integridad)
-- ---------------------------------------------

IF OBJECT_ID('CORRELATIVA_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE CORRELATIVA_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE CORRELATIVA_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ID_MATERIA, ID_CORRELATIVA, DVH FROM CORRELATIVA ORDER BY ID;
END
GO

IF OBJECT_ID('CORRELATIVA_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE CORRELATIVA_ACTUALIZAR_DVH
GO
CREATE PROCEDURE CORRELATIVA_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CORRELATIVA SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — ALUMNO_MATERIA (integridad)
-- ---------------------------------------------

IF OBJECT_ID('ALUMNO_MATERIA_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE ALUMNO_MATERIA_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ID_USUARIO, ID_MATERIA, ESTADO,
           NOTA_PARCIAL1, NOTA_PARCIAL2, NOTA_RECUPERATORIO, NOTA_FINAL,
           FECHA_FINAL, FECHA_RECUPERATORIO, DVH
    FROM   ALUMNO_MATERIA
    ORDER  BY ID;
END
GO

IF OBJECT_ID('ALUMNO_MATERIA_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_ACTUALIZAR_DVH
GO
CREATE PROCEDURE ALUMNO_MATERIA_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ALUMNO_MATERIA SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — EVENTO_ACADEMICO (integridad)
-- ---------------------------------------------

IF OBJECT_ID('EVENTO_ACADEMICO_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE EVENTO_ACADEMICO_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE EVENTO_ACADEMICO_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ID_MATERIA, ID_USUARIO, TIPO, DESCRIPCION, FECHA, PESO, DVH
    FROM   EVENTO_ACADEMICO
    ORDER  BY ID;
END
GO

IF OBJECT_ID('EVENTO_ACADEMICO_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE EVENTO_ACADEMICO_ACTUALIZAR_DVH
GO
CREATE PROCEDURE EVENTO_ACADEMICO_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE EVENTO_ACADEMICO SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — PERIODO_ACADEMICO (integridad)
-- ---------------------------------------------

IF OBJECT_ID('PERIODO_ACADEMICO_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE PERIODO_ACADEMICO_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE PERIODO_ACADEMICO_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ANIO, CUATRIMESTRE, DESCRIPCION, FECHA_INICIO, FECHA_FIN, DVH
    FROM   PERIODO_ACADEMICO
    ORDER  BY ID;
END
GO

IF OBJECT_ID('PERIODO_ACADEMICO_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE PERIODO_ACADEMICO_ACTUALIZAR_DVH
GO
CREATE PROCEDURE PERIODO_ACADEMICO_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PERIODO_ACADEMICO SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — INSCRIPCION (integridad)
-- ---------------------------------------------

IF OBJECT_ID('INSCRIPCION_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE INSCRIPCION_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ID_USUARIO, ID_PERIODO, FECHA_INSCRIPCION, DVH
    FROM   INSCRIPCION
    ORDER  BY ID;
END
GO

IF OBJECT_ID('INSCRIPCION_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_ACTUALIZAR_DVH
GO
CREATE PROCEDURE INSCRIPCION_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE INSCRIPCION SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — INSCRIPCION_DETALLE (integridad)
-- ---------------------------------------------

IF OBJECT_ID('INSCRIPCION_DETALLE_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_DETALLE_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE INSCRIPCION_DETALLE_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ID_INSCRIPCION, ID_MATERIA, DVH FROM INSCRIPCION_DETALLE ORDER BY ID;
END
GO

IF OBJECT_ID('INSCRIPCION_DETALLE_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_DETALLE_ACTUALIZAR_DVH
GO
CREATE PROCEDURE INSCRIPCION_DETALLE_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE INSCRIPCION_DETALLE SET DVH = @dvh WHERE ID = @id;
END
GO

-- ---------------------------------------------
-- Tabla y procedimientos — snapshot del último estado válido conocido
-- (uno por tabla, JSON con {ID, Atributos[]} de cada fila). Se guarda cada
-- vez que se recalcula la integridad de una tabla y se usa para diagnosticar
-- con precisión (campo a campo) qué cambió cuando se detecta una alteración.
-- ---------------------------------------------

IF OBJECT_ID('SNAPSHOT_INTEGRIDAD', 'U') IS NULL
CREATE TABLE SNAPSHOT_INTEGRIDAD (
    TABLA      VARCHAR(50)   NOT NULL PRIMARY KEY,
    DATOS_JSON NVARCHAR(MAX) NOT NULL,
    FECHA      DATETIME      NOT NULL DEFAULT GETDATE()
)
GO

IF OBJECT_ID('SNAPSHOT_OBTENER', 'P') IS NOT NULL DROP PROCEDURE SNAPSHOT_OBTENER
GO
CREATE PROCEDURE SNAPSHOT_OBTENER
    @tabla VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DATOS_JSON FROM SNAPSHOT_INTEGRIDAD WHERE TABLA = @tabla;
END
GO

IF OBJECT_ID('SNAPSHOT_GUARDAR', 'P') IS NOT NULL DROP PROCEDURE SNAPSHOT_GUARDAR
GO
CREATE PROCEDURE SNAPSHOT_GUARDAR
    @tabla      VARCHAR(50),
    @datosJson  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM SNAPSHOT_INTEGRIDAD WHERE TABLA = @tabla)
        UPDATE SNAPSHOT_INTEGRIDAD SET DATOS_JSON = @datosJson, FECHA = GETDATE() WHERE TABLA = @tabla;
    ELSE
        INSERT INTO SNAPSHOT_INTEGRIDAD (TABLA, DATOS_JSON) VALUES (@tabla, @datosJson);
END
GO

PRINT '✔ Integridad DVH/DVV generalizada instalada correctamente.';
GO

-- ---------------------------------------------
-- STORED PROCEDURES — RESTORE (mecanismo de Restaurar base de datos)
-- Dos procedimientos uniformes por cada una de las 9 tablas de negocio:
--   {TABLA}_ELIMINAR_POR_ID   — borra por ID (fase de borrado, filas agregadas externamente)
--   {TABLA}_RESTAURAR_FILA    — upsert por ID con IDENTITY_INSERT (fase de upsert,
--                                filas modificadas o eliminadas externamente que vuelven)
-- Usados por BLL.IntegridadBLL.RestaurarTodasLasTablas().
-- ---------------------------------------------

-- ===== USUARIO =====

IF OBJECT_ID('USUARIO_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE USUARIO_ELIMINAR_POR_ID
GO
CREATE PROCEDURE USUARIO_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM USUARIO_PERFIL WHERE USUARIO_ID = @id;
    DELETE FROM USUARIO WHERE ID = @id;
END
GO

IF OBJECT_ID('USUARIO_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE USUARIO_RESTAURAR_FILA
GO
CREATE PROCEDURE USUARIO_RESTAURAR_FILA
    @id        INT,
    @usuario   VARCHAR(50),
    @pass      VARCHAR(64),
    @direccion VARBINARY(256),
    @activo    BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM USUARIO WHERE ID = @id)
        UPDATE USUARIO
        SET USUARIO = @usuario, PASS = @pass, DIRECCION = @direccion, ACTIVO = @activo
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT USUARIO ON;
        INSERT INTO USUARIO (ID, USUARIO, PASS, DIRECCION, ACTIVO) VALUES (@id, @usuario, @pass, @direccion, @activo);
        SET IDENTITY_INSERT USUARIO OFF;
    END
END
GO

-- ===== MATERIA =====

IF OBJECT_ID('MATERIA_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE MATERIA_ELIMINAR_POR_ID
GO
CREATE PROCEDURE MATERIA_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM MATERIA WHERE ID = @id;
END
GO

IF OBJECT_ID('MATERIA_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE MATERIA_RESTAURAR_FILA
GO
CREATE PROCEDURE MATERIA_RESTAURAR_FILA
    @id        INT,
    @nombre    VARCHAR(100),
    @codigo    VARCHAR(20),
    @modalidad VARCHAR(20),
    @peso      INT,
    @activa    BIT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM MATERIA WHERE ID = @id)
        UPDATE MATERIA
        SET NOMBRE = @nombre, CODIGO = @codigo, MODALIDAD = @modalidad, PESO = @peso, ACTIVA = @activa
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT MATERIA ON;
        INSERT INTO MATERIA (ID, NOMBRE, CODIGO, MODALIDAD, PESO, ACTIVA) VALUES (@id, @nombre, @codigo, @modalidad, @peso, @activa);
        SET IDENTITY_INSERT MATERIA OFF;
    END
END
GO

-- ===== PERIODO_ACADEMICO =====

IF OBJECT_ID('PERIODO_ACADEMICO_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE PERIODO_ACADEMICO_ELIMINAR_POR_ID
GO
CREATE PROCEDURE PERIODO_ACADEMICO_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM PERIODO_ACADEMICO WHERE ID = @id;
END
GO

IF OBJECT_ID('PERIODO_ACADEMICO_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE PERIODO_ACADEMICO_RESTAURAR_FILA
GO
CREATE PROCEDURE PERIODO_ACADEMICO_RESTAURAR_FILA
    @id           INT,
    @anio         INT,
    @cuatrimestre INT,
    @descripcion  VARCHAR(100),
    @fechaInicio  DATE,
    @fechaFin     DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM PERIODO_ACADEMICO WHERE ID = @id)
        UPDATE PERIODO_ACADEMICO
        SET ANIO = @anio, CUATRIMESTRE = @cuatrimestre, DESCRIPCION = @descripcion,
            FECHA_INICIO = @fechaInicio, FECHA_FIN = @fechaFin
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT PERIODO_ACADEMICO ON;
        INSERT INTO PERIODO_ACADEMICO (ID, ANIO, CUATRIMESTRE, DESCRIPCION, FECHA_INICIO, FECHA_FIN)
        VALUES (@id, @anio, @cuatrimestre, @descripcion, @fechaInicio, @fechaFin);
        SET IDENTITY_INSERT PERIODO_ACADEMICO OFF;
    END
END
GO

-- ===== BITACORA =====

IF OBJECT_ID('BITACORA_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE BITACORA_ELIMINAR_POR_ID
GO
CREATE PROCEDURE BITACORA_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM BITACORA WHERE ID = @id;
END
GO

IF OBJECT_ID('BITACORA_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE BITACORA_RESTAURAR_FILA
GO
CREATE PROCEDURE BITACORA_RESTAURAR_FILA
    @id      INT,
    @usuario VARCHAR(50),
    @accion  VARCHAR(100),
    @fecha   DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM BITACORA WHERE ID = @id)
        UPDATE BITACORA SET USUARIO = @usuario, ACCION = @accion, FECHA = @fecha WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT BITACORA ON;
        INSERT INTO BITACORA (ID, USUARIO, ACCION, FECHA) VALUES (@id, @usuario, @accion, @fecha);
        SET IDENTITY_INSERT BITACORA OFF;
    END
END
GO

-- ===== CORRELATIVA =====
-- PK física sigue siendo (ID_MATERIA, ID_CORRELATIVA); la columna ID
-- IDENTITY agregada en la tarea anterior se usa igual como clave del upsert.

IF OBJECT_ID('CORRELATIVA_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE CORRELATIVA_ELIMINAR_POR_ID
GO
CREATE PROCEDURE CORRELATIVA_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM CORRELATIVA WHERE ID = @id;
END
GO

IF OBJECT_ID('CORRELATIVA_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE CORRELATIVA_RESTAURAR_FILA
GO
CREATE PROCEDURE CORRELATIVA_RESTAURAR_FILA
    @id            INT,
    @idMateria     INT,
    @idCorrelativa INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM CORRELATIVA WHERE ID = @id)
        UPDATE CORRELATIVA SET ID_MATERIA = @idMateria, ID_CORRELATIVA = @idCorrelativa WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT CORRELATIVA ON;
        INSERT INTO CORRELATIVA (ID, ID_MATERIA, ID_CORRELATIVA) VALUES (@id, @idMateria, @idCorrelativa);
        SET IDENTITY_INSERT CORRELATIVA OFF;
    END
END
GO

-- ===== ALUMNO_MATERIA =====

IF OBJECT_ID('ALUMNO_MATERIA_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_ELIMINAR_POR_ID
GO
CREATE PROCEDURE ALUMNO_MATERIA_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ALUMNO_MATERIA WHERE ID = @id;
END
GO

IF OBJECT_ID('ALUMNO_MATERIA_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE ALUMNO_MATERIA_RESTAURAR_FILA
GO
CREATE PROCEDURE ALUMNO_MATERIA_RESTAURAR_FILA
    @id                  INT,
    @idUsuario           INT,
    @idMateria           INT,
    @estado              VARCHAR(30),
    @notaParcial1        DECIMAL(4,2),
    @notaParcial2        DECIMAL(4,2),
    @notaRecuperatorio   DECIMAL(4,2),
    @notaFinal           DECIMAL(4,2),
    @fechaFinal          DATETIME,
    @fechaRecuperatorio  DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ALUMNO_MATERIA WHERE ID = @id)
        UPDATE ALUMNO_MATERIA
        SET ID_USUARIO = @idUsuario, ID_MATERIA = @idMateria, ESTADO = @estado,
            NOTA_PARCIAL1 = @notaParcial1, NOTA_PARCIAL2 = @notaParcial2,
            NOTA_RECUPERATORIO = @notaRecuperatorio, NOTA_FINAL = @notaFinal,
            FECHA_FINAL = @fechaFinal, FECHA_RECUPERATORIO = @fechaRecuperatorio
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT ALUMNO_MATERIA ON;
        INSERT INTO ALUMNO_MATERIA (ID, ID_USUARIO, ID_MATERIA, ESTADO, NOTA_PARCIAL1, NOTA_PARCIAL2,
                                     NOTA_RECUPERATORIO, NOTA_FINAL, FECHA_FINAL, FECHA_RECUPERATORIO)
        VALUES (@id, @idUsuario, @idMateria, @estado, @notaParcial1, @notaParcial2,
                @notaRecuperatorio, @notaFinal, @fechaFinal, @fechaRecuperatorio);
        SET IDENTITY_INSERT ALUMNO_MATERIA OFF;
    END
END
GO

-- ===== EVENTO_ACADEMICO =====

IF OBJECT_ID('EVENTO_ACADEMICO_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE EVENTO_ACADEMICO_ELIMINAR_POR_ID
GO
CREATE PROCEDURE EVENTO_ACADEMICO_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM EVENTO_ACADEMICO WHERE ID = @id;
END
GO

IF OBJECT_ID('EVENTO_ACADEMICO_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE EVENTO_ACADEMICO_RESTAURAR_FILA
GO
CREATE PROCEDURE EVENTO_ACADEMICO_RESTAURAR_FILA
    @id          INT,
    @idMateria   INT,
    @idUsuario   INT,
    @tipo        VARCHAR(30),
    @descripcion VARCHAR(200),
    @fecha       DATETIME,
    @peso        INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM EVENTO_ACADEMICO WHERE ID = @id)
        UPDATE EVENTO_ACADEMICO
        SET ID_MATERIA = @idMateria, ID_USUARIO = @idUsuario, TIPO = @tipo,
            DESCRIPCION = @descripcion, FECHA = @fecha, PESO = @peso
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT EVENTO_ACADEMICO ON;
        INSERT INTO EVENTO_ACADEMICO (ID, ID_MATERIA, ID_USUARIO, TIPO, DESCRIPCION, FECHA, PESO)
        VALUES (@id, @idMateria, @idUsuario, @tipo, @descripcion, @fecha, @peso);
        SET IDENTITY_INSERT EVENTO_ACADEMICO OFF;
    END
END
GO

-- ===== INSCRIPCION =====

IF OBJECT_ID('INSCRIPCION_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_ELIMINAR_POR_ID
GO
CREATE PROCEDURE INSCRIPCION_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM INSCRIPCION WHERE ID = @id;
END
GO

IF OBJECT_ID('INSCRIPCION_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_RESTAURAR_FILA
GO
CREATE PROCEDURE INSCRIPCION_RESTAURAR_FILA
    @id                INT,
    @idUsuario         INT,
    @idPeriodo         INT,
    @fechaInscripcion  DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM INSCRIPCION WHERE ID = @id)
        UPDATE INSCRIPCION
        SET ID_USUARIO = @idUsuario, ID_PERIODO = @idPeriodo, FECHA_INSCRIPCION = @fechaInscripcion
        WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT INSCRIPCION ON;
        INSERT INTO INSCRIPCION (ID, ID_USUARIO, ID_PERIODO, FECHA_INSCRIPCION)
        VALUES (@id, @idUsuario, @idPeriodo, @fechaInscripcion);
        SET IDENTITY_INSERT INSCRIPCION OFF;
    END
END
GO

-- ===== INSCRIPCION_DETALLE =====

IF OBJECT_ID('INSCRIPCION_DETALLE_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_DETALLE_ELIMINAR_POR_ID
GO
CREATE PROCEDURE INSCRIPCION_DETALLE_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM INSCRIPCION_DETALLE WHERE ID = @id;
END
GO

IF OBJECT_ID('INSCRIPCION_DETALLE_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE INSCRIPCION_DETALLE_RESTAURAR_FILA
GO
CREATE PROCEDURE INSCRIPCION_DETALLE_RESTAURAR_FILA
    @id            INT,
    @idInscripcion INT,
    @idMateria     INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM INSCRIPCION_DETALLE WHERE ID = @id)
        UPDATE INSCRIPCION_DETALLE SET ID_INSCRIPCION = @idInscripcion, ID_MATERIA = @idMateria WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT INSCRIPCION_DETALLE ON;
        INSERT INTO INSCRIPCION_DETALLE (ID, ID_INSCRIPCION, ID_MATERIA) VALUES (@id, @idInscripcion, @idMateria);
        SET IDENTITY_INSERT INSCRIPCION_DETALLE OFF;
    END
END
GO

PRINT '✔ Restore de integridad (ELIMINAR_POR_ID / RESTAURAR_FILA) instalado correctamente.';
GO

-- =============================================
-- Bloqueo de cuenta por intentos fallidos — portado de "ingenieria_software"
-- (las columnas INTENTOS_FALLIDOS/BLOQUEADO se agregan más arriba, junto a
-- DVH de USUARIO, porque USUARIO_LISTAR_PARA_INTEGRIDAD las necesita antes)
-- =============================================

IF OBJECT_ID('USUARIO_VERIFICAR_BLOQUEO', 'P') IS NOT NULL DROP PROCEDURE USUARIO_VERIFICAR_BLOQUEO
GO
CREATE PROCEDURE USUARIO_VERIFICAR_BLOQUEO
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BLOQUEADO FROM USUARIO WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_INCREMENTAR_INTENTOS', 'P') IS NOT NULL DROP PROCEDURE USUARIO_INCREMENTAR_INTENTOS
GO
CREATE PROCEDURE USUARIO_INCREMENTAR_INTENTOS
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO
    SET INTENTOS_FALLIDOS = INTENTOS_FALLIDOS + 1,
        BLOQUEADO = CASE WHEN INTENTOS_FALLIDOS + 1 >= 3 THEN 1 ELSE BLOQUEADO END
    WHERE USUARIO = @usuario;

    SELECT BLOQUEADO FROM USUARIO WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_RESETEAR_INTENTOS', 'P') IS NOT NULL DROP PROCEDURE USUARIO_RESETEAR_INTENTOS
GO
CREATE PROCEDURE USUARIO_RESETEAR_INTENTOS
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET INTENTOS_FALLIDOS = 0 WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_DESBLOQUEAR', 'P') IS NOT NULL DROP PROCEDURE USUARIO_DESBLOQUEAR
GO
CREATE PROCEDURE USUARIO_DESBLOQUEAR
    @usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET BLOQUEADO = 0, INTENTOS_FALLIDOS = 0 WHERE USUARIO = @usuario;
END
GO

IF OBJECT_ID('USUARIO_LISTAR_BLOQUEADOS', 'P') IS NOT NULL DROP PROCEDURE USUARIO_LISTAR_BLOQUEADOS
GO
CREATE PROCEDURE USUARIO_LISTAR_BLOQUEADOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, INTENTOS_FALLIDOS FROM USUARIO WHERE BLOQUEADO = 1 ORDER BY USUARIO;
END
GO

PRINT '✔ Bloqueo de cuenta por intentos fallidos instalado correctamente.';
GO

-- =============================================
-- Bitácora: filtros + paginado (CRITICIDAD ya fue agregada más arriba,
-- junto a DVH de BITACORA, porque BITACORA_LISTAR_PARA_INTEGRIDAD la
-- necesitaba antes)
-- =============================================

IF OBJECT_ID('BITACORA_INSERTAR', 'P') IS NOT NULL DROP PROCEDURE BITACORA_INSERTAR
GO
CREATE PROCEDURE BITACORA_INSERTAR
    @usuario    VARCHAR(50),
    @accion     VARCHAR(100),
    @criticidad VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO BITACORA (USUARIO, ACCION, FECHA, CRITICIDAD) VALUES (@usuario, @accion, GETDATE(), @criticidad);
END
GO

IF OBJECT_ID('BITACORA_LISTAR_USUARIOS_DISTINCT', 'P') IS NOT NULL DROP PROCEDURE BITACORA_LISTAR_USUARIOS_DISTINCT
GO
CREATE PROCEDURE BITACORA_LISTAR_USUARIOS_DISTINCT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT USUARIO FROM BITACORA ORDER BY USUARIO;
END
GO

IF OBJECT_ID('BITACORA_LISTAR_PAGINADO', 'P') IS NOT NULL DROP PROCEDURE BITACORA_LISTAR_PAGINADO
GO
CREATE PROCEDURE BITACORA_LISTAR_PAGINADO
    @usuario     VARCHAR(50) = NULL,
    @criticidad  VARCHAR(10) = NULL,
    @fecha_desde DATETIME    = NULL,
    @fecha_hasta DATETIME    = NULL,
    @pagina      INT         = 1,
    @tamanio     INT         = 50
AS
BEGIN
    SET NOCOUNT ON;
    IF @pagina  < 1 SET @pagina  = 1;
    IF @tamanio < 1 SET @tamanio = 50;

    SELECT ID, USUARIO, ACCION, FECHA, CRITICIDAD,
           COUNT(*) OVER() AS TOTAL_FILAS
    FROM BITACORA
    WHERE (@usuario     IS NULL OR USUARIO    = @usuario)
      AND (@criticidad  IS NULL OR CRITICIDAD = @criticidad)
      AND (@fecha_desde IS NULL OR FECHA     >= @fecha_desde)
      AND (@fecha_hasta IS NULL OR FECHA     <  @fecha_hasta)
    ORDER BY FECHA DESC
    OFFSET (@pagina - 1) * @tamanio ROWS
    FETCH NEXT @tamanio ROWS ONLY;
END
GO

PRINT '✔ Bitácora: filtros y paginado instalados correctamente.';
GO

-- =============================================
-- ===== PERFILES Y PERMISOS (Composite) =====
-- Bloque idempotente. Reemplaza la columna USUARIO.ROL (texto fijo) por
-- perfiles dinámicos: un perfil (ROL) agrupa permisos y puede contener otros
-- perfiles (PADRE_ID); un usuario puede tener varios perfiles
-- (USUARIO_PERFIL). Los permisos efectivos se resuelven en la capa BLL
-- recorriendo el árbol (Composite), no con una consulta plana.
--
-- Este script solo define esquema y procedimientos. Los DATOS de seguridad
-- (catálogo de permisos, perfiles base Administrador/Webmaster/Alumno con sus
-- permisos, y los usuarios admin y webmaster) los siembra la aplicación al
-- iniciar (BLL.SiembraBLL), para que cada fila nazca con su DVH/DVV calculado
-- por el motor de integridad de C#.
--
-- Sobre una base existente: crea las tablas, anota en MIGRACION_USUARIO_ROL el
-- perfil que corresponde a cada usuario según su USUARIO.ROL y elimina esa
-- columna; la aplicación asigna esos perfiles en su primer arranque.
--
-- REGLA: toda sentencia de este script que cambie filas de una tabla con
-- integridad (DVH/DVV) descarta el DVV y el snapshot de esa tabla solo si
-- cambió algo (@@ROWCOUNT > 0); el arranque de la aplicación la recalcula.
-- Si no, la verificación vería esos cambios como alteraciones.
-- =============================================

USE BDUNIVERSIDAD
GO

-- ---------------------------------------------
-- Tablas. ROL_PERMISO y USUARIO_PERFIL tienen PK compuesta; se les agrega
-- una columna ID IDENTITY de respaldo (igual que CORRELATIVA) para reutilizar
-- el motor genérico de integridad, que trabaja con un ID entero por fila.
-- ---------------------------------------------

IF OBJECT_ID('ROL', 'U') IS NULL
CREATE TABLE ROL (
    ID        INT IDENTITY(1,1) NOT NULL,
    NOMBRE    VARCHAR(100)      NOT NULL,
    PADRE_ID  INT               NULL,
    PROTEGIDO BIT               NOT NULL DEFAULT 0,
    DVH       INT               NOT NULL DEFAULT 0,
    CONSTRAINT PK_ROL        PRIMARY KEY (ID),
    CONSTRAINT UQ_ROL_NOMBRE UNIQUE (NOMBRE),
    CONSTRAINT FK_ROL_PADRE  FOREIGN KEY (PADRE_ID) REFERENCES ROL(ID)
)
GO

IF OBJECT_ID('PERMISO', 'U') IS NULL
CREATE TABLE PERMISO (
    ID     INT IDENTITY(1,1) NOT NULL,
    NOMBRE VARCHAR(100)      NOT NULL,
    DVH    INT               NOT NULL DEFAULT 0,
    CONSTRAINT PK_PERMISO        PRIMARY KEY (ID),
    CONSTRAINT UQ_PERMISO_NOMBRE UNIQUE (NOMBRE)
)
GO

IF OBJECT_ID('ROL_PERMISO', 'U') IS NULL
CREATE TABLE ROL_PERMISO (
    ID         INT IDENTITY(1,1) NOT NULL,
    ROL_ID     INT               NOT NULL,
    PERMISO_ID INT               NOT NULL,
    DVH        INT               NOT NULL DEFAULT 0,
    CONSTRAINT PK_ROL_PERMISO         PRIMARY KEY (ROL_ID, PERMISO_ID),
    CONSTRAINT UQ_ROL_PERMISO_ID      UNIQUE (ID),
    CONSTRAINT FK_ROL_PERMISO_ROL     FOREIGN KEY (ROL_ID)     REFERENCES ROL(ID),
    CONSTRAINT FK_ROL_PERMISO_PERMISO FOREIGN KEY (PERMISO_ID) REFERENCES PERMISO(ID)
)
GO

IF OBJECT_ID('USUARIO_PERFIL', 'U') IS NULL
CREATE TABLE USUARIO_PERFIL (
    ID         INT IDENTITY(1,1) NOT NULL,
    USUARIO_ID INT               NOT NULL,
    ROL_ID     INT               NOT NULL,
    DVH        INT               NOT NULL DEFAULT 0,
    CONSTRAINT PK_USUARIO_PERFIL         PRIMARY KEY (USUARIO_ID, ROL_ID),
    CONSTRAINT UQ_USUARIO_PERFIL_ID      UNIQUE (ID),
    CONSTRAINT FK_USUARIO_PERFIL_USUARIO FOREIGN KEY (USUARIO_ID) REFERENCES USUARIO(ID),
    CONSTRAINT FK_USUARIO_PERFIL_ROL     FOREIGN KEY (ROL_ID)     REFERENCES ROL(ID)
)
GO

-- ---------------------------------------------
-- Tabla de paso para migrar USUARIO.ROL -> USUARIO_PERFIL. Los perfiles base
-- (ROL) y sus permisos ya no se siembran en este script: los crea la aplicación
-- al iniciar (SiembraBLL). Como al migrar todavía no existen, se guarda acá el
-- nombre del perfil que le corresponde a cada usuario y la aplicación lo asigna
-- (y vacía la tabla) en su primer arranque.
-- ---------------------------------------------

IF OBJECT_ID('MIGRACION_USUARIO_ROL', 'U') IS NULL
CREATE TABLE MIGRACION_USUARIO_ROL (
    USUARIO_ID INT          NOT NULL PRIMARY KEY,
    ROL_NOMBRE VARCHAR(100) NOT NULL
)
GO

-- ---------------------------------------------
-- Migración de USUARIO.ROL (a MIGRACION_USUARIO_ROL), y baja de la columna.
-- Dinámico porque en una instalación nueva la columna ya no existe.
-- ---------------------------------------------

IF COL_LENGTH('USUARIO', 'ROL') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        INSERT INTO MIGRACION_USUARIO_ROL (USUARIO_ID, ROL_NOMBRE)
        SELECT u.ID, CASE u.ROL
                         WHEN ''Admin''     THEN ''Administrador''
                         WHEN ''Webmaster'' THEN ''Webmaster''
                         ELSE ''Alumno''
                     END
        FROM USUARIO u
        WHERE NOT EXISTS (SELECT 1 FROM MIGRACION_USUARIO_ROL m WHERE m.USUARIO_ID = u.ID)
          AND NOT EXISTS (SELECT 1 FROM USUARIO_PERFIL up WHERE up.USUARIO_ID = u.ID)';

    DECLARE @df SYSNAME = (
        SELECT dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('USUARIO') AND c.name = 'ROL');
    IF @df IS NOT NULL
        EXEC('ALTER TABLE USUARIO DROP CONSTRAINT [' + @df + ']');

    EXEC('ALTER TABLE USUARIO DROP COLUMN ROL');

    -- El orden de columnas de USUARIO cambió: los DVH/DVV y el snapshot que
    -- había quedan obsoletos y se regeneran al recalcular la integridad.
    DELETE FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = 'USUARIO';
    DELETE FROM SNAPSHOT_INTEGRIDAD         WHERE TABLA = 'USUARIO';
    DELETE FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = 'USUARIO_PERFIL';
    DELETE FROM SNAPSHOT_INTEGRIDAD         WHERE TABLA = 'USUARIO_PERFIL';
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — perfiles y permisos
-- ---------------------------------------------

IF OBJECT_ID('ROL_LISTAR_TODOS', 'P') IS NOT NULL DROP PROCEDURE ROL_LISTAR_TODOS
GO
CREATE PROCEDURE ROL_LISTAR_TODOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, PADRE_ID, PROTEGIDO FROM ROL ORDER BY ID;
END
GO

IF OBJECT_ID('ROL_PERMISO_LISTAR_TODOS', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_LISTAR_TODOS
GO
CREATE PROCEDURE ROL_PERMISO_LISTAR_TODOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT rp.ROL_ID, rp.PERMISO_ID, p.NOMBRE
    FROM   ROL_PERMISO rp
    JOIN   PERMISO     p ON p.ID = rp.PERMISO_ID
    ORDER  BY rp.ROL_ID, p.NOMBRE;
END
GO

IF OBJECT_ID('ROL_CAMBIAR_PADRE', 'P') IS NOT NULL DROP PROCEDURE ROL_CAMBIAR_PADRE
GO
CREATE PROCEDURE ROL_CAMBIAR_PADRE
    @id       INT,
    @padre_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ROL SET PADRE_ID = @padre_id WHERE ID = @id;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_LISTAR_POR_USUARIO', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_LISTAR_POR_USUARIO
GO
CREATE PROCEDURE USUARIO_PERFIL_LISTAR_POR_USUARIO
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ROL_ID FROM USUARIO_PERFIL WHERE USUARIO_ID = @usuario_id;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_ASIGNAR', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_ASIGNAR
GO
CREATE PROCEDURE USUARIO_PERFIL_ASIGNAR
    @usuario_id INT,
    @rol_id     INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM USUARIO_PERFIL WHERE USUARIO_ID = @usuario_id AND ROL_ID = @rol_id)
        INSERT INTO USUARIO_PERFIL (USUARIO_ID, ROL_ID) VALUES (@usuario_id, @rol_id);
END
GO

-- ---------------------------------------------
-- STORED PROCEDURES — integridad y restore de las 4 tablas nuevas
-- ---------------------------------------------

-- ===== ROL =====

IF OBJECT_ID('ROL_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE ROL_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE ROL_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, PADRE_ID, PROTEGIDO, DVH FROM ROL ORDER BY ID;
END
GO

IF OBJECT_ID('ROL_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE ROL_ACTUALIZAR_DVH
GO
CREATE PROCEDURE ROL_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ROL SET DVH = @dvh WHERE ID = @id;
END
GO

IF OBJECT_ID('ROL_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE ROL_ELIMINAR_POR_ID
GO
CREATE PROCEDURE ROL_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Se sueltan los hijos para no violar la FK autorreferencia; la fase de
    -- upsert del restore les vuelve a poner su padre original.
    UPDATE ROL SET PADRE_ID = NULL WHERE PADRE_ID = @id;
    DELETE FROM ROL WHERE ID = @id;
END
GO

IF OBJECT_ID('ROL_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE ROL_RESTAURAR_FILA
GO
CREATE PROCEDURE ROL_RESTAURAR_FILA
    @id        INT,
    @nombre    VARCHAR(100),
    @padreId   INT,
    @protegido BIT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ROL WHERE ID = @id)
        UPDATE ROL SET NOMBRE = @nombre, PADRE_ID = @padreId, PROTEGIDO = @protegido WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT ROL ON;
        INSERT INTO ROL (ID, NOMBRE, PADRE_ID, PROTEGIDO) VALUES (@id, @nombre, @padreId, @protegido);
        SET IDENTITY_INSERT ROL OFF;
    END
END
GO

-- ===== PERMISO =====

IF OBJECT_ID('PERMISO_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE PERMISO_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE PERMISO_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE, DVH FROM PERMISO ORDER BY ID;
END
GO

IF OBJECT_ID('PERMISO_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE PERMISO_ACTUALIZAR_DVH
GO
CREATE PROCEDURE PERMISO_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PERMISO SET DVH = @dvh WHERE ID = @id;
END
GO

IF OBJECT_ID('PERMISO_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE PERMISO_ELIMINAR_POR_ID
GO
CREATE PROCEDURE PERMISO_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM PERMISO WHERE ID = @id;
END
GO

IF OBJECT_ID('PERMISO_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE PERMISO_RESTAURAR_FILA
GO
CREATE PROCEDURE PERMISO_RESTAURAR_FILA
    @id     INT,
    @nombre VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM PERMISO WHERE ID = @id)
        UPDATE PERMISO SET NOMBRE = @nombre WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT PERMISO ON;
        INSERT INTO PERMISO (ID, NOMBRE) VALUES (@id, @nombre);
        SET IDENTITY_INSERT PERMISO OFF;
    END
END
GO

-- ===== ROL_PERMISO =====

IF OBJECT_ID('ROL_PERMISO_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE ROL_PERMISO_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ROL_ID, PERMISO_ID, DVH FROM ROL_PERMISO ORDER BY ID;
END
GO

IF OBJECT_ID('ROL_PERMISO_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_ACTUALIZAR_DVH
GO
CREATE PROCEDURE ROL_PERMISO_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ROL_PERMISO SET DVH = @dvh WHERE ID = @id;
END
GO

IF OBJECT_ID('ROL_PERMISO_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_ELIMINAR_POR_ID
GO
CREATE PROCEDURE ROL_PERMISO_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ROL_PERMISO WHERE ID = @id;
END
GO

IF OBJECT_ID('ROL_PERMISO_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_RESTAURAR_FILA
GO
CREATE PROCEDURE ROL_PERMISO_RESTAURAR_FILA
    @id        INT,
    @rolId     INT,
    @permisoId INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ROL_PERMISO WHERE ID = @id)
        UPDATE ROL_PERMISO SET ROL_ID = @rolId, PERMISO_ID = @permisoId WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT ROL_PERMISO ON;
        INSERT INTO ROL_PERMISO (ID, ROL_ID, PERMISO_ID) VALUES (@id, @rolId, @permisoId);
        SET IDENTITY_INSERT ROL_PERMISO OFF;
    END
END
GO

-- ===== USUARIO_PERFIL =====

IF OBJECT_ID('USUARIO_PERFIL_LISTAR_PARA_INTEGRIDAD', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_LISTAR_PARA_INTEGRIDAD
GO
CREATE PROCEDURE USUARIO_PERFIL_LISTAR_PARA_INTEGRIDAD
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO_ID, ROL_ID, DVH FROM USUARIO_PERFIL ORDER BY ID;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_ACTUALIZAR_DVH', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_ACTUALIZAR_DVH
GO
CREATE PROCEDURE USUARIO_PERFIL_ACTUALIZAR_DVH
    @id  INT,
    @dvh INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO_PERFIL SET DVH = @dvh WHERE ID = @id;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_ELIMINAR_POR_ID', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_ELIMINAR_POR_ID
GO
CREATE PROCEDURE USUARIO_PERFIL_ELIMINAR_POR_ID
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM USUARIO_PERFIL WHERE ID = @id;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_RESTAURAR_FILA', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_RESTAURAR_FILA
GO
CREATE PROCEDURE USUARIO_PERFIL_RESTAURAR_FILA
    @id        INT,
    @usuarioId INT,
    @rolId     INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM USUARIO_PERFIL WHERE ID = @id)
        UPDATE USUARIO_PERFIL SET USUARIO_ID = @usuarioId, ROL_ID = @rolId WHERE ID = @id;
    ELSE
    BEGIN
        SET IDENTITY_INSERT USUARIO_PERFIL ON;
        INSERT INTO USUARIO_PERFIL (ID, USUARIO_ID, ROL_ID) VALUES (@id, @usuarioId, @rolId);
        SET IDENTITY_INSERT USUARIO_PERFIL OFF;
    END
END
GO

PRINT '✔ Perfiles y permisos (Composite) instalados correctamente.';
PRINT 'Iniciar la aplicación para sembrar los datos base y calcular la integridad.';
GO

-- =============================================
-- ===== GESTIÓN DE PERFILES Y ASIGNACIÓN =====
-- Procedimientos de escritura y lectura usados por Perfiles.aspx y
-- AsignarPerfiles.aspx. Las reglas de negocio (ciclos, perfiles
-- protegidos, último operador) viven en la capa BLL.
-- =============================================

USE BDUNIVERSIDAD
GO

IF OBJECT_ID('PERMISO_LISTAR_TODOS', 'P') IS NOT NULL DROP PROCEDURE PERMISO_LISTAR_TODOS
GO
CREATE PROCEDURE PERMISO_LISTAR_TODOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, NOMBRE FROM PERMISO ORDER BY NOMBRE;
END
GO

IF OBJECT_ID('USUARIO_LISTAR_TODOS', 'P') IS NOT NULL DROP PROCEDURE USUARIO_LISTAR_TODOS
GO
CREATE PROCEDURE USUARIO_LISTAR_TODOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, USUARIO, BLOQUEADO, ACTIVO FROM USUARIO ORDER BY USUARIO;
END
GO

IF OBJECT_ID('USUARIO_PERFIL_LISTAR_TODOS', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_LISTAR_TODOS
GO
CREATE PROCEDURE USUARIO_PERFIL_LISTAR_TODOS
AS
BEGIN
    SET NOCOUNT ON;
    SELECT USUARIO_ID, ROL_ID FROM USUARIO_PERFIL;
END
GO

IF OBJECT_ID('ROL_INSERTAR', 'P') IS NOT NULL DROP PROCEDURE ROL_INSERTAR
GO
CREATE PROCEDURE ROL_INSERTAR
    @nombre    VARCHAR(100),
    @padre_id  INT = NULL,
    @protegido BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ROL (NOMBRE, PADRE_ID, PROTEGIDO) VALUES (@nombre, @padre_id, @protegido);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ID;
END
GO

IF OBJECT_ID('ROL_RENOMBRAR', 'P') IS NOT NULL DROP PROCEDURE ROL_RENOMBRAR
GO
CREATE PROCEDURE ROL_RENOMBRAR
    @id     INT,
    @nombre VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ROL SET NOMBRE = @nombre WHERE ID = @id;
END
GO

-- Elimina el perfil y sus asignaciones de permisos. La BLL ya verificó que
-- no sea protegido, no tenga hijos ni usuarios asignados.
IF OBJECT_ID('ROL_ELIMINAR', 'P') IS NOT NULL DROP PROCEDURE ROL_ELIMINAR
GO
CREATE PROCEDURE ROL_ELIMINAR
    @id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
        DELETE FROM ROL_PERMISO WHERE ROL_ID = @id;
        DELETE FROM ROL WHERE ID = @id;
    COMMIT TRANSACTION;
END
GO

IF OBJECT_ID('ROL_PERMISO_ELIMINAR_POR_ROL', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_ELIMINAR_POR_ROL
GO
CREATE PROCEDURE ROL_PERMISO_ELIMINAR_POR_ROL
    @rol_id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ROL_PERMISO WHERE ROL_ID = @rol_id;
END
GO

IF OBJECT_ID('ROL_PERMISO_INSERTAR', 'P') IS NOT NULL DROP PROCEDURE ROL_PERMISO_INSERTAR
GO
CREATE PROCEDURE ROL_PERMISO_INSERTAR
    @rol_id     INT,
    @permiso_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ROL_PERMISO WHERE ROL_ID = @rol_id AND PERMISO_ID = @permiso_id)
        INSERT INTO ROL_PERMISO (ROL_ID, PERMISO_ID) VALUES (@rol_id, @permiso_id);
END
GO

IF OBJECT_ID('USUARIO_PERFIL_ELIMINAR_POR_USUARIO', 'P') IS NOT NULL DROP PROCEDURE USUARIO_PERFIL_ELIMINAR_POR_USUARIO
GO
CREATE PROCEDURE USUARIO_PERFIL_ELIMINAR_POR_USUARIO
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM USUARIO_PERFIL WHERE USUARIO_ID = @usuario_id;
END
GO

-- ---------------------------------------------
-- Gestión de usuarios (alta, baja lógica)
-- ---------------------------------------------

-- Con la integridad inválida solo el Webmaster puede ingresar: se le quita el permiso
-- "Ingresar con integridad invalida" al Administrador en bases que lo tenían sembrado.
-- El permiso sigue existiendo (un perfil propio puede recibirlo).
DELETE rp
FROM ROL_PERMISO rp
JOIN ROL     r ON r.ID = rp.ROL_ID
JOIN PERMISO p ON p.ID = rp.PERMISO_ID
WHERE r.NOMBRE = 'Administrador' AND p.NOMBRE = 'Ingresar con integridad invalida'

IF @@ROWCOUNT > 0
BEGIN
    DELETE FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = 'ROL_PERMISO';
    DELETE FROM SNAPSHOT_INTEGRIDAD         WHERE TABLA = 'ROL_PERMISO';
END
GO

IF OBJECT_ID('PERMISO_INSERTAR', 'P') IS NOT NULL DROP PROCEDURE PERMISO_INSERTAR
GO
CREATE PROCEDURE PERMISO_INSERTAR
    @nombre VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PERMISO (NOMBRE) VALUES (@nombre);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ID;
END
GO

IF OBJECT_ID('MIGRACION_USUARIO_ROL_LISTAR', 'P') IS NOT NULL DROP PROCEDURE MIGRACION_USUARIO_ROL_LISTAR
GO
CREATE PROCEDURE MIGRACION_USUARIO_ROL_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT USUARIO_ID, ROL_NOMBRE FROM MIGRACION_USUARIO_ROL;
END
GO

IF OBJECT_ID('MIGRACION_USUARIO_ROL_ELIMINAR', 'P') IS NOT NULL DROP PROCEDURE MIGRACION_USUARIO_ROL_ELIMINAR
GO
CREATE PROCEDURE MIGRACION_USUARIO_ROL_ELIMINAR
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM MIGRACION_USUARIO_ROL WHERE USUARIO_ID = @usuario_id;
END
GO

-- Marcador de siembra: existe una entrada SIEMBRA_DATOS_BASE en la bitácora.
-- Mientras no exista, la aplicación siembra perfiles y usuarios iniciales; una vez
-- que existe, nunca los vuelve a crear (el administrador pudo borrarlos a propósito).
IF OBJECT_ID('BITACORA_EXISTE_ACCION', 'P') IS NOT NULL DROP PROCEDURE BITACORA_EXISTE_ACCION
GO
CREATE PROCEDURE BITACORA_EXISTE_ACCION
    @accion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE WHEN EXISTS (SELECT 1 FROM BITACORA WHERE ACCION = @accion) THEN 1 ELSE 0 END AS EXISTE;
END
GO

-- Descarta el DVV y el snapshot de una tabla con integridad, dentro de la misma
-- transacción de un seed que le insertó filas: si el recálculo posterior falla, la
-- tabla queda sin DVV: la aplicación la recalcula en el próximo intento del mismo
-- proceso; si el proceso se reinicia antes, queda inválida hasta que el Webmaster la
-- recalcule desde Integridad.aspx.
IF OBJECT_ID('INTEGRIDAD_INVALIDAR_TABLA', 'P') IS NOT NULL DROP PROCEDURE INTEGRIDAD_INVALIDAR_TABLA
GO
CREATE PROCEDURE INTEGRIDAD_INVALIDAR_TABLA
    @tabla VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM DIGITO_VERIFICADOR_VERTICAL WHERE TABLA = @tabla;
    DELETE FROM SNAPSHOT_INTEGRIDAD         WHERE TABLA = @tabla;
END
GO

IF OBJECT_ID('USUARIO_INSERTAR', 'P') IS NOT NULL DROP PROCEDURE USUARIO_INSERTAR
GO
CREATE PROCEDURE USUARIO_INSERTAR
    @usuario   VARCHAR(50),
    @pass      VARCHAR(64),
    @direccion VARBINARY(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO USUARIO (USUARIO, PASS, DIRECCION) VALUES (@usuario, @pass, @direccion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ID;
END
GO

IF OBJECT_ID('USUARIO_ACTUALIZAR_ESTADO', 'P') IS NOT NULL DROP PROCEDURE USUARIO_ACTUALIZAR_ESTADO
GO
CREATE PROCEDURE USUARIO_ACTUALIZAR_ESTADO
    @id     INT,
    @activo BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE USUARIO SET ACTIVO = @activo WHERE ID = @id;
END
GO

PRINT '✔ Gestión de perfiles y asignación instalada correctamente.';
GO
