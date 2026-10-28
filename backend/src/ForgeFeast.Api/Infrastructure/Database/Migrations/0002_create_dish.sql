-- F01 Cardápio da semana: pratos por dia útil (ADR-012).
-- As regras de negócio são validadas na API; as constraints abaixo são a última linha de defesa.
-- CK_Dish_Date_Weekday: 1900-01-01 foi uma segunda-feira, então o resto da divisão por 7 vale
-- 0 (segunda) a 4 (sexta), sem depender de SET DATEFIRST nem do idioma da sessão.
CREATE TABLE dbo.Dish
(
    Id          INT IDENTITY(1, 1) NOT NULL,
    [Date]      DATE               NOT NULL,
    Name        NVARCHAR(100)      NOT NULL,
    Description NVARCHAR(300)      NULL,
    CONSTRAINT PK_Dish PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Dish_Name_NotBlank CHECK (LEN(Name) > 0),
    CONSTRAINT CK_Dish_Date_Weekday CHECK (((DATEDIFF(DAY, '19000101', [Date]) % 7) + 7) % 7 < 5)
);

-- Consulta da semana: WHERE [Date] BETWEEN @WeekStart AND @WeekEnd ORDER BY [Date], Id.
CREATE NONCLUSTERED INDEX IX_Dish_Date
    ON dbo.Dish ([Date])
    INCLUDE (Name, Description);
