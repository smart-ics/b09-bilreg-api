using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.SalesContext.ResepFeature;

public record EtiketType
{
    private EtiketType(
        string signa,
        string instruction,
        int frequency,
        decimal unitDose,
        string route,
        string mealRelation,
        string uddTime,
        string note)
    {
        Signa = signa;
        Instruction = instruction;
        Frequency = frequency;
        UnitDose = unitDose;
        Route = route;
        MealRelation = mealRelation;
        UddTime = uddTime;
        Note = note;
    }

    public string Signa { get; init; }
    public string Instruction { get; init; }
    public int Frequency { get; init; }
    public decimal UnitDose { get; init; }
    public string Route { get; init; }
    public string MealRelation { get; init; }
    public string UddTime { get; init; }
    public string Note { get; init; }

    public static EtiketType Default => new(
        AppConst.DASH,
        AppConst.DASH,
        0,
        0,
        AppConst.DASH,
        AppConst.DASH,
        AppConst.DASH,
        AppConst.DASH);

    public static EtiketType Create(string signa, string instruction, string note = AppConst.DASH)
        => Create(signa, instruction, 0, 0m, AppConst.DASH, AppConst.DASH, AppConst.DASH, note);


    public static EtiketType Create(
        string signa,
        string instruction,
        int? frequency,
        decimal? unitDose,
        string route = AppConst.DASH,
        string mealRelation = AppConst.DASH,
        string uddTime = AppConst.DASH,
        string note = AppConst.DASH)
    {
        var safeSigna = string.IsNullOrWhiteSpace(signa) ? AppConst.DASH : signa;
        var safeInstruction = string.IsNullOrWhiteSpace(instruction) ? AppConst.DASH : instruction;
        var safeRoute = string.IsNullOrWhiteSpace(route) ? AppConst.DASH : route;
        var safeMealRelation = string.IsNullOrWhiteSpace(mealRelation) ? AppConst.DASH : mealRelation;
        var safeUddTime = string.IsNullOrWhiteSpace(uddTime) ? AppConst.DASH : uddTime;
        var safeNote = string.IsNullOrWhiteSpace(note) ? AppConst.DASH : note;

        var actualFrequency = frequency ?? 0;
        var actualUnitDose = unitDose ?? 0;

        if (actualFrequency <= 0 || actualUnitDose <= 0)
        {
            var (parsedDailyDose, parsedConsumeAmount) = ParseSignaFallback(safeSigna, safeInstruction);
            if (actualFrequency <= 0)
                actualFrequency = parsedDailyDose;
            if (actualUnitDose <= 0)
                actualUnitDose = parsedConsumeAmount;
        }

        return new EtiketType(
            safeSigna,
            safeInstruction,
            actualFrequency,
            actualUnitDose,
            safeRoute,
            safeMealRelation,
            safeUddTime,
            safeNote);
    }

    public static EtiketType Load(
        string signa,
        string instruction,
        int frequency,
        decimal unitDose,
        string note = AppConst.DASH)
        => Load(signa, instruction, frequency, unitDose, AppConst.DASH, AppConst.DASH, AppConst.DASH, note);

    public static EtiketType Load(
        string signa,
        string instruction,
        int frequency,
        decimal unitDose,
        string route,
        string mealRelation,
        string uddTime,
        string note = AppConst.DASH)
    {
        var safeSigna = string.IsNullOrWhiteSpace(signa) ? AppConst.DASH : signa;
        var safeInstruction = string.IsNullOrWhiteSpace(instruction) ? AppConst.DASH : instruction;
        var safeRoute = string.IsNullOrWhiteSpace(route) ? AppConst.DASH : route;
        var safeMealRelation = string.IsNullOrWhiteSpace(mealRelation) ? AppConst.DASH : mealRelation;
        var safeUddTime = string.IsNullOrWhiteSpace(uddTime) ? AppConst.DASH : uddTime;
        var safeNote = string.IsNullOrWhiteSpace(note) ? AppConst.DASH : note;

        return new EtiketType(
            safeSigna,
            safeInstruction,
            frequency,
            unitDose,
            safeRoute,
            safeMealRelation,
            safeUddTime,
            safeNote);
    }

    private static (int DailyDose, decimal ConsumeAmount) ParseSignaFallback(string signa, string instruction)
    {
        SignaType? parsedSigna = null;

        if (!string.IsNullOrWhiteSpace(instruction) && instruction != AppConst.DASH)
        {
            try
            {
                parsedSigna = SignaParser.Parse(instruction);
            }
            catch (FormatException)
            {
                parsedSigna = null;
            }

            if (parsedSigna is { DailyDose: 0, ConsumeAmount: 0 })
                parsedSigna = null;
        }

        if (parsedSigna is null && !string.IsNullOrWhiteSpace(signa) && signa != AppConst.DASH)
        {
            try
            {
                parsedSigna = SignaParser.Parse(signa);
            }
            catch (FormatException)
            {
                parsedSigna = null;
            }

            if (parsedSigna is { DailyDose: 0, ConsumeAmount: 0 })
                parsedSigna = null;
        }

        if (parsedSigna != null)
            return (parsedSigna.DailyDose, parsedSigna.ConsumeAmount);

        var candidate = !string.IsNullOrWhiteSpace(instruction) && instruction != AppConst.DASH
            ? instruction
            : signa;
        parsedSigna = SignaParser.Parse(candidate);
        return (parsedSigna.DailyDose, parsedSigna.ConsumeAmount);
    }
}