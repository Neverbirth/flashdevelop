using ASCompletion.Model;

namespace AS3Context.Utils
{
    static class ClassModelExtensions
    {
        static public bool IsAssignableFrom(this ClassModel src, ClassModel dst)
        {
            if ((src.Flags & FlagType.Interface) > 0)
                return IsInterfaceAssignableFrom(src.QualifiedName, dst);
            if ((src.Flags & FlagType.Final) == 0)
                return IsClassAssignableFrom(src.QualifiedName, dst);
            return src.QualifiedName == dst.QualifiedName;
        }

        static private bool IsClassAssignableFrom(string srcType, ClassModel dst)
        {
            dst.ResolveExtends();
            do
            {
                if (dst.QualifiedName == srcType)
                {
                    return true;
                }

                dst = dst.Extends;
            } while (!dst.IsVoid());

            return false;
        }

        static private bool IsInterfaceAssignableFrom(string srcType, ClassModel dst)
        {
            dst.ResolveExtends();
            do
            {
                if (dst.Implements != null)
                {
                    foreach (var implement in dst.Implements)
                    {
                        var interfaceModel = MxmlComplete.context.ResolveType(implement, dst.InFile);
                        if (interfaceModel.QualifiedName == srcType) return true;
                        if (IsInterfaceAssignableFrom(srcType, interfaceModel))
                        {
                            return true;
                        }
                    }
                }

                dst = dst.Extends;
            } while (!dst.IsVoid());

            return false;
        }
    }
}
