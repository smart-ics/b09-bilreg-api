﻿namespace Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;

public class JenisTarifModel(string id, string name) : IJenisTarifKey
{
    public string JenisTarifId { get; protected set; } = id;
    public string JenisTarifName { get; protected set; } = name;
}   

