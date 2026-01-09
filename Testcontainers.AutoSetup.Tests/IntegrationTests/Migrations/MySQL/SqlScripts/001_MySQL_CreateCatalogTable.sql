CREATE DATABASE `RawSql_CatalogTest`;

CREATE TABLE `RawSql_CatalogTest`.`Catalog` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `Name` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(255) NULL
);