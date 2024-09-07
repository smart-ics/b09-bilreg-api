namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg
{
    public class LayananDkModel(
     string  layananDkId,
     string  layananDkName,
     decimal rawatInapCode,
     decimal rawatJalanCode,
     decimal kesehatanJiwaCode,
     decimal bedahCode,
     decimal rujukanCode,
     decimal kunjunganRumahCode,
     decimal layananSubCode
     
        ) :ILayananDkKey
    {
        public string LayananDkId { get; protected set; } = layananDkId ;
        public string LayananDkName { get; protected set; } = layananDkName ;
        public decimal RawatInapCode { get; protected set; } = rawatInapCode ;
        public decimal RawatJalanCode { get; protected set; } = rawatJalanCode ;
        public decimal KesehatanJiwaCode { get; protected set; } = kesehatanJiwaCode ;
        public decimal BedahCode { get; protected set; } = bedahCode ;
        public decimal RujukanCode { get; protected set; } = rujukanCode ;
        public decimal KunjunganRumahCode { get; protected set; } = kunjunganRumahCode ;
        public decimal LayananSubCode { get; protected set; } = layananSubCode;
    }

    public interface ILayananDkKey
    {
        public string LayananDkId { get; }
    }
}
