namespace Hessian
{
    public class ClassDef
    {
        public int RefId { get; private set; }
        public string Name { get; private set; }
        public string[] Fields { get; private set; }

        public ClassDef(int refId, string name, string[] fields)
        {
            RefId = refId;
            Name = name;
            Fields = fields;
        }
    }
}
