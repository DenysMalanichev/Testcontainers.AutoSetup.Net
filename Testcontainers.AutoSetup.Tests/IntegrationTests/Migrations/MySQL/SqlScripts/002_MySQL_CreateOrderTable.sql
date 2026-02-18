CREATE TABLE `RawSql_CatalogTest`.`Order` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `CatalogId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `TotalPrice` INT NOT NULL,
    CONSTRAINT fk_parent_id
        FOREIGN KEY (`CatalogId`) 
        REFERENCES `RawSql_CatalogTest`.`Catalog` (`Id`)
);