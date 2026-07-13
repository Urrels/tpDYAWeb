-- Elimina stored procedures huérfanos de BDUNIVERSIDAD (sin ningún caller en el código).
-- DVV_GUARDAR y DVV_OBTENER fueron superados por DVV_ACTUALIZAR y DVV_LISTAR (ver IntegridadDAL.cs).
-- CORRELATIVA_INSERTAR y CORRELATIVA_ELIMINAR quedaron sin caller al borrar
-- MateriaDAL.InsertarCorrelativa/EliminarCorrelativa (sin ABM de correlativas en la UI).
-- Ejecutar una sola vez contra la base ya desplegada.

USE BDUNIVERSIDAD
GO

IF OBJECT_ID('DVV_GUARDAR','P') IS NOT NULL DROP PROCEDURE DVV_GUARDAR
GO

IF OBJECT_ID('DVV_OBTENER','P') IS NOT NULL DROP PROCEDURE DVV_OBTENER
GO

IF OBJECT_ID('CORRELATIVA_INSERTAR','P') IS NOT NULL DROP PROCEDURE CORRELATIVA_INSERTAR
GO

IF OBJECT_ID('CORRELATIVA_ELIMINAR','P') IS NOT NULL DROP PROCEDURE CORRELATIVA_ELIMINAR
GO
