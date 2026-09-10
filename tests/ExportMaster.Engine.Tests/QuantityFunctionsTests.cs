using System.Numerics;
using ExportMaster.Engine.Data;

namespace ExportMaster.Engine.Tests;

/// <summary>Величины, извлекаемые из комплексного значения.</summary>
public class QuantityFunctionsTests
{
    [Fact]
    public void AmplitudeIsExpressedInDecibels()
    {
        // В примере ТЗ п. 4.4.3.3 результат AMP подписан «дБ»,
        // поэтому логарифм входит в саму функцию.
        Assert.Equal(-20, QuantityFunctions.Evaluate("AMP", new Complex(0.1, 0))!.Value, 10);
        Assert.Equal(0, QuantityFunctions.Evaluate("AMP", new Complex(1, 0))!.Value, 10);
    }

    [Fact]
    public void LinearMagnitudeIsAvailableSeparately()
    {
        Assert.Equal(5, QuantityFunctions.Evaluate("MAG", new Complex(3, 4))!.Value, 10);
    }

    [Fact]
    public void PhaseIsInDegrees()
    {
        Assert.Equal(90, QuantityFunctions.Evaluate("PHASE", new Complex(0, 1))!.Value, 10);
    }

    [Fact]
    public void PartsAreTakenAsIs()
    {
        Assert.Equal(3, QuantityFunctions.Evaluate("RE", new Complex(3, 4))!.Value);
        Assert.Equal(4, QuantityFunctions.Evaluate("IM", new Complex(3, 4))!.Value);
    }

    [Fact]
    public void UnknownNameGivesNothing()
    {
        Assert.Null(QuantityFunctions.Evaluate("AMPL", Complex.One));
        Assert.False(QuantityFunctions.IsKnown("AMPL"));
    }
}
