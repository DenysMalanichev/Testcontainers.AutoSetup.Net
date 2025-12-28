CREATE DATABASE [RawSql_CatalogTest];
GO

USE [RawSql_CatalogTest];

CREATE TABLE Catalog (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL
);
GO