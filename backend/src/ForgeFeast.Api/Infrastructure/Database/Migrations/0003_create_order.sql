-- F02 Carrinho e pedido individual: cabeçalho do pedido e seus itens (ADR-016).
-- As regras de negócio são validadas na API; as constraints abaixo são a última linha de defesa.
-- [Order] é palavra reservada do T-SQL: todo SQL usa dbo.[Order] entre colchetes.
-- CustomerName guarda o nome já com trim feito pela API. O collation CI (sem diferenciar maiúsculas,
-- com acentos) é explícito para a unicidade não depender do collation do servidor (RF-06).
CREATE TABLE dbo.[Order]
(
    Id            INT IDENTITY(1, 1)                          NOT NULL,
    CustomerName  NVARCHAR(60) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [Date]        DATE                                        NOT NULL,
    CreatedAtUtc  DATETIME2(0)                                NOT NULL,
    UpdatedAtUtc  DATETIME2(0)                                NOT NULL,
    CONSTRAINT PK_Order PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Order_CustomerName_Date UNIQUE (CustomerName, [Date]),
    CONSTRAINT CK_Order_CustomerName_NotBlank CHECK (LEN(CustomerName) > 0),
    -- Mesma fórmula de CK_Dish_Date_Weekday: 1900-01-01 foi uma segunda-feira.
    CONSTRAINT CK_Order_Date_Weekday CHECK (((DATEDIFF(DAY, '19000101', [Date]) % 7) + 7) % 7 < 5)
);

-- Consolidação por data (F03).
CREATE NONCLUSTERED INDEX IX_Order_Date
    ON dbo.[Order] ([Date]);

-- A PK composta impede prato repetido no mesmo pedido.
-- FK para Dish sem cascata: o banco bloqueia a remoção de prato com pedidos (RF-10).
CREATE TABLE dbo.OrderItem
(
    OrderId  INT     NOT NULL,
    DishId   INT     NOT NULL,
    Quantity TINYINT NOT NULL,
    CONSTRAINT PK_OrderItem PRIMARY KEY CLUSTERED (OrderId, DishId),
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order] (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItem_Dish FOREIGN KEY (DishId) REFERENCES dbo.Dish (Id),
    CONSTRAINT CK_OrderItem_Quantity CHECK (Quantity BETWEEN 1 AND 10)
);

CREATE NONCLUSTERED INDEX IX_OrderItem_DishId
    ON dbo.OrderItem (DishId);
