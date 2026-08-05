using Ardalis.GuardClauses;
using Farinv.Domain.Shared.Helpers;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.SalesContext.ResepFeature;

public record EtiketType
{
    private EtiketType(string signa, string instruction, int frequency, decimal unitDose, string note)
    {
        Guard.Against.NullOrWhiteSpace(instruction, "Instruction cannot be empty");

        Signa = signa;
        Instruction = instruction;
        Frequency = frequency;
        UnitDose = unitDose;
        Note = note;
    }

    public static EtiketType Create(string signa, string instruction, string note)
    {
        var parseEtiket = SignaParser.Parse(instruction);
        if (parseEtiket is { DailyDose: 0, ConsumeAmount: 0 })
            parseEtiket = SignaParser.Parse(signa);

        var etiket = new EtiketType(signa, instruction, parseEtiket.DailyDose, parseEtiket.ConsumeAmount, note);
        return etiket;
    }
    
    public static EtiketType Default => new (AppConst.DASH, AppConst.DASH, 0, 0, AppConst.DASH);
    
    public string Signa { get; init; }
    public string Instruction { get; init; }
    public int Frequency { get; init; }
    public decimal UnitDose { get; init; }
    public string Note { get; init; }
}