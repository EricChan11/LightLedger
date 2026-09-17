
public sealed class CurrencyItem {
    public readonly string Code;
    public CurrencyItem(string code) { Code=code; }
    public override string ToString() { return L.Currency(Code); }
}
