USE `RawSql_CatalogTest`;

INSERT INTO `Catalog` (`Name`, `Description`) VALUES ('Item1', 'Description for Item 1');
INSERT INTO `Catalog` (`Name`, `Description`) VALUES ('Item2', 'Description for Item 2');
INSERT INTO `Catalog` (`Name`, `Description`) VALUES ('Item3', 'Description for Item 3');

INSERT INTO `Order` (`CatalogId`, `Quantity`, `TotalPrice`) VALUES (1, 5, 100);
INSERT INTO `Order` (`CatalogId`, `Quantity`, `TotalPrice`) VALUES (2, 3, 75);
INSERT INTO `Order` (`CatalogId`, `Quantity`, `TotalPrice`) VALUES (3, 2, 50);