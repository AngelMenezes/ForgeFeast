using ForgeFeast.Api.Infrastructure.Configuration;

namespace ForgeFeast.Api.UnitTests.Infrastructure.Configuration;

public sealed class DotEnvLoaderTests
{
    [Fact]
    public void Parse_CommentsAndBlankLines_AreIgnored()
    {
        var lines = new[] { "# comentário", "", "   ", "CHAVE=valor" };

        var result = DotEnvLoader.Parse(lines).ToList();

        var pair = Assert.Single(result);
        Assert.Equal("CHAVE", pair.Key);
        Assert.Equal("valor", pair.Value);
    }

    [Fact]
    public void Parse_ValueWithEqualsAndSemicolons_KeepsEverythingAfterFirstEquals()
    {
        var lines = new[] { "ConnectionStrings__ForgeFeast=Server=localhost,1433;Database=ForgeFeast" };

        var pair = Assert.Single(DotEnvLoader.Parse(lines));

        Assert.Equal("ConnectionStrings__ForgeFeast", pair.Key);
        Assert.Equal("Server=localhost,1433;Database=ForgeFeast", pair.Value);
    }

    [Fact]
    public void Parse_QuotedValue_RemovesSurroundingQuotes()
    {
        var pair = Assert.Single(DotEnvLoader.Parse(["SENHA=\"com espaço\""]));

        Assert.Equal("com espaço", pair.Value);
    }

    [Fact]
    public void Load_VariableAlreadyDefined_DoesNotOverride()
    {
        var existingKey = $"FORGEFEAST_TEST_{Guid.NewGuid():N}";
        var newKey = $"FORGEFEAST_TEST_{Guid.NewGuid():N}";
        var file = Path.GetTempFileName();

        try
        {
            Environment.SetEnvironmentVariable(existingKey, "do-processo");
            File.WriteAllLines(file, [$"{existingKey}=do-arquivo", $"{newKey}=novo"]);

            var applied = DotEnvLoader.Load(file);

            Assert.Equal(1, applied);
            Assert.Equal("do-processo", Environment.GetEnvironmentVariable(existingKey));
            Assert.Equal("novo", Environment.GetEnvironmentVariable(newKey));
        }
        finally
        {
            Environment.SetEnvironmentVariable(existingKey, null);
            Environment.SetEnvironmentVariable(newKey, null);
            File.Delete(file);
        }
    }
}
