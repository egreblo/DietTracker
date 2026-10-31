using DietTracker.Core;

namespace DietTracker.Tests;

public class MeasureParserTests
{
    [Theory]
    [InlineData("200g", null, 200)]
    [InlineData("200 g", null, 200)]
    [InlineData("1.2 kg", null, 1200)]
    [InlineData("400ml", null, 400)]
    [InlineData("2 tbsp", null, 30)]
    [InlineData("1/2 tsp", null, 2.5)]
    [InlineData("1 1/2 cups", null, 360)]
    [InlineData("1½ tbsp", null, 22.5)]
    [InlineData("3 žlice", null, 45)]
    [InlineData("1,5 dl", null, 150)]
    [InlineData("8 oz", null, 226.8)]
    [InlineData("1 tin", null, 400)]
    [InlineData("1 (400g) can", null, 400)]
    [InlineData("2 x 400g", null, 800)]
    [InlineData("2-3 tbsp", null, 30)]
    [InlineData("5 thinly sliced", 110.0, 550)]
    [InlineData("2 cloves", 3.0, 6)]
    public void ParsesCommonMeasures(string measure, double? piece, double expected)
    {
        Assert.Equal(expected, MeasureParser.ParseGrams(measure, piece)!.Value, 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("to taste")]
    [InlineData("Juice of 1/2")]
    [InlineData("handful")]
    public void ReturnsNullWhenQuantityUnknown(string? measure)
    {
        Assert.Null(MeasureParser.ParseGrams(measure, 100));
    }

    [Fact]
    public void CountWithoutPieceWeightIsUnknown()
    {
        Assert.Null(MeasureParser.ParseGrams("3 large", null));
    }
}
