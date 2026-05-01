using System;
using System.Drawing;

namespace ProjectDataLib
{
    public interface ITag
    {
        Guid Id { get; set; }

        Guid ParentId { get; set; }

        string Name { get; set; }
        Boolean ActName { get; }

        int DevAdress { get; set; }
        Boolean ActDevAdress { get; }

        int BlockAdress { get; set; }
        Boolean ActBlockAdress { get; }

        int Adress { get; set; }
        Boolean ActAdress { get; }

        int BitByte { get; set; }
        Boolean ActBitByte { get; }

        TypeData TypeData_ { get; set; }
        Boolean ActTypeData_ { get; }

        BytesOrder BytesOrder_ { get; set; }
        Boolean ActBytesOrder_ { get; }

        string AreaData { get; set; }
        Boolean ActAreaData { get; }

        Object Value { get; set; }
        Boolean ActValue { get; }

        string Description { get; set; }
        Boolean ActDscription { get; }

        string TagVersion { get; set; }
        Boolean ActVersion { get; }

        Boolean ActParam { get; }

        Boolean ActSetValue { get; }

        void SetValue(object val);

        Boolean ActClr { get; }
        Color Clr { get; set; }

        Boolean ActWidth { get; }
        int Width { get; set; }

        Boolean ActGrEnable { get; }

        Boolean GrEnable { get; set; }

        Boolean ActGrVisible { get; }
        Boolean GrVisible { get; set; }

        Boolean ActGrAxisKey { get; }
        string GrAxisKey { get; set; }

        Boolean ActGrMarkers { get; }

        Boolean ActGrVisibleTab { get; }

        Boolean GrVisibleTab { get; set; }

        Type getOwnType();

        string GetFormatedValue();
    }
}