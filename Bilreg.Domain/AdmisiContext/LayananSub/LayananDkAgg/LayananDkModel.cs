namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg
{
    public class LayananDkModel(
        string  layananDkId,
        string  layananDkName,
        int rawatInapCode,
        int rawatJalanCode,
        int kesehatanJiwaCode,
        int bedahCode,
        int rujukanCode,
        int kunjunganRumahCode,
        int layananSubCode) :ILayananDkKey
    {
        public string LayananDkId { get; protected set; } = layananDkId ;
        public string LayananDkName { get; protected set; } = layananDkName ;
        public int RawatInapCode { get; protected set; } = rawatInapCode ;
        public int RawatJalanCode { get; protected set; } = rawatJalanCode ;
        public int KesehatanJiwaCode { get; protected set; } = kesehatanJiwaCode ;
        public int BedahCode { get; protected set; } = bedahCode ;
        public int RujukanCode { get; protected set; } = rujukanCode ;
        public int KunjunganRumahCode { get; protected set; } = kunjunganRumahCode ;
        public int LayananSubCode { get; protected set; } = layananSubCode;
    }

    public interface ILayananDkKey
    {
        public string LayananDkId { get; }
    }
}
