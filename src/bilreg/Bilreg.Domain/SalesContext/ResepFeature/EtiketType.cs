using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.SalesContext.ResepFeature;

public record EtiketType
{
    private EtiketType(string signa, string instruction, int frequency, decimal unitDose, string note)
    {
        Signa = signa;
        Instruction = instruction;
        Frequency = frequency;
        UnitDose = unitDose;
        Note = note;
    }

    public static EtiketType Create(string signa, string instruction, string note)
    {
        SignaType parsedSigna;
        if (!string.IsNullOrWhiteSpace(instruction) && instruction != AppConst.DASH)
        {
            parsedSigna = SignaParser.Parse(instruction);
            if (parsedSigna is { DailyDose: 0, ConsumeAmount: 0 })
                parsedSigna = SignaParser.Parse(signa);
        }
        else
        {
            parsedSigna = SignaParser.Parse(signa);
        }

        var etiket = new EtiketType(signa, instruction, parsedSigna.DailyDose, parsedSigna.ConsumeAmount, note);
        return etiket;
    }

    public static EtiketType Load(string signa, string instruction, int frequency, decimal unitDose, string note)
    {
        var safeSigna = string.IsNullOrWhiteSpace(signa) ? AppConst.DASH : signa;
        var safeInstruction = string.IsNullOrWhiteSpace(instruction) ? AppConst.DASH : instruction;
        var safeNote = string.IsNullOrWhiteSpace(note) ? AppConst.DASH : note;
        return new EtiketType(safeSigna, safeInstruction, frequency, unitDose, safeNote);
    }
    
    public static EtiketType Default => new (AppConst.DASH, AppConst.DASH, 0, 0, AppConst.DASH);
    
    public string Signa { get; init; }
    public string Instruction { get; init; }
    public int Frequency { get; init; }
    public decimal UnitDose { get; init; }
    public string Note { get; init; }
}