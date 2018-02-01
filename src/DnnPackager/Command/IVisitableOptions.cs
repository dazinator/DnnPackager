namespace DnnPackager.Command
{
    public interface IVisitableOptions
    {
        void Accept(ICommandVisitor visitor);
    }
}
