using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class LinkedEquipDictionary
{
    public static string[] linkedList42 = new string[]
    {
        "42c_aim9ex1;fa26_aim9e;",
        "42c_aim9ex2;fa26_aim9ex2;",
        "agm89x1;fa26_agm89x1;",
        "av42_agm161;fa26_agm161;f45_agm161;f45_agm161Internal;",
        "av42_gbu12x1;fa26_gbu12x1;f45_gbu12x1;",
        "av42_gbu12x2;fa26_gbu12x2;f45_gbu12x2Internal;",
        "av42_gbu12x3;fa26_gbu12x3;",
        "cagm-6;fa26_cagm-6;",
        "cbu97x1;fa26_cbu97x1;",
        "gbu38x1;fa26_gbu38x1;f45_gbu38x1;",
        "gbu38x2;fa26_gbu38x2;f45_gbu38x2Internal;",
        "gbu38x3;fa26_gbu38x3;",
        "gbu39x3;",
        "gbu39x4u;fa26_gbu39x4uFront;fa26_gbu39x4uRear;f45-gbu39;",
        "hellfirex4;",
        "iris-t-x1;fa26_iris-t-x1;",
        "iris-t-x2;fa26_iris-t-x2;",
        "iris-t-x3;fa26_iris-t-x3;",
        "marmx1;",
        "maverickx1;af_maverickx1;fa26_maverickx1;",
        "maverickx3;af_maverickx3;fa26_maverickx3;",
        "mk82HDx1;fa26_mk82HDx1;",
        "mk82HDx2;fa26_mk82HDx2;",
        "mk82HDx3;fa26_mk82HDx3;",
        "mk82x1;af_mk82;f45_mk82x1;",
        "mk82x2;fa26_mk82x2;f45_mk82Internal;",
        "mk82x3;fa26_mk82x3;",
        "sidearmx1;fa26_sidearmx1;",
        "sidearmx2;fa26_sidearmx2;",
        "sidearmx3;fa26_sidearmx3;",
        "sidewinderx1;af_aim9;",
        "sidewinderx2;fa26_aim9x2;",
        "sidewinderx3;fa26_aim9x3;"
    };
    public static string[] linkedList26 = new string[]
    {
        "af_aim9;sidewinderx1;",
        "af_amraam;",
        "af_amraamRail;",
        "af_amraamRailx2;",
        "af_maverickx1;maverickx1;",
        "af_maverickx3;maverickx3;",
        "af_mk82;mk82x1;f45_mk82x1;",
        "fa26_agm161;av42_agm161;f45_agm161;f45_agm161Internal;",
        "fa26_agm89x1;agm89x1;",
        "fa26_aim7;",
        "fa26_aim7Rail;",
        "fa26_aim9e;42c_aim9ex1;",
        "fa26_aim9ex2;42c_aim9ex2;",
        "fa26_aim9x2;sidewinderx2;",
        "fa26_aim9x3;sidewinderx3;",
        "fa26_cagm-6;cagm-6;",
        "fa26_cbu97x1;cbu97x1;",
        "fa26_gbu12x1;av42_gbu12x1;f45_gbu12x1;",
        "fa26_gbu12x2;av42_gbu12x2;f45_gbu12x2Internal;",
        "fa26_gbu12x3;av42_gbu12x3;",
        "fa26_gbu38x1;gbu38x1;f45_gbu38x1;",
        "fa26_gbu38x2;gbu38x2;f45_gbu38x2Internal;",
        "fa26_gbu38x3;gbu38x3;",
        "fa26_gbu39x4uFront;gbu39x4u;f45-gbu39;",
        "fa26_gbu39x4uRear;gbu39x4u;f45-gbu39;",
        "fa26_harmx1;",
        "fa26_harmx1dpMount;",
        "fa26_iris-t-x1;iris-t-x1;",
        "fa26_iris-t-x2;iris-t-x2;",
        "fa26_iris-t-x3;iris-t-x3;",
        "fa26_maverickx1;maverickx1;",
        "fa26_maverickx3;maverickx3;",
        "fa26_mk82HDx1;mk82HDx1;",
        "fa26_mk82HDx2;mk82HDx2;",
        "fa26_mk82HDx3;mk82HDx3;",
        "fa26_mk82x2;mk82x2;f45_mk82Internal;",
        "fa26_mk82x3;mk82x3;",
        "fa26_mk83x1;f45_mk83x1;f45_mk83x1Internal;",
        "fa26_sidearmx1;sidearmx1;",
        "fa26_sidearmx2;sidearmx2;",
        "fa26_sidearmx3;sidearmx3;",
    };

    public static string[] linkedList45 = new string[]
    {
        "f45-agm145I;",
        "f45-agm145ISide;",
        "f45-agm145x3;",
        "f45-gbu39;gbu39x4u;fa26_gbu39x4uFront;fa26_gbu39x4uRear;",
        "f45-gbu53;",
        "f45_agm161;av42_agm161;fa26_agm161;",
        "f45_agm161Internal;av42_agm161;fa26_agm161;",
        "f45_aim9x1;",
        "f45_amraamInternal;",
        "f45_amraamRail;",
        "f45_gbu12x1;av42_gbu12x1;fa26_gbu12x1;",
        "f45_gbu12x2Internal;av42_gbu12x2;fa26_gbu12x2;",
        "f45_gbu38x1;gbu38x1;fa26_gbu38x1;",
        "f45_gbu38x2Internal;gbu38x2;fa26_gbu38x2;",
        "f45_gbu38x4Internal;",
        "f45_mk82Internal;mk82x2;fa26_mk82x2;",
        "f45_mk82x1;mk82x1;af_mk82;",
        "f45_mk82x4Internal;",
        "f45_mk83x1;fa26_mk83x1;",
        "f45_mk83x1Internal;fa26_mk83x1;",
        "f45_sidewinderx2;"
    };

    public static bool isAllowed(string name, VTOLVehicles compareAgainst)
    {
        string[] toCompare;
        switch (compareAgainst)
        {
            case VTOLVehicles.AV42C:
                toCompare = linkedList42;
                break;
            case VTOLVehicles.FA26B:
                toCompare = linkedList26;
                break;
            case VTOLVehicles.F45A:
                toCompare = linkedList45;
                break;
            default:
                toCompare = null;
                break;
        }
        foreach (string other in toCompare)
        {
            if (other.Contains(name))
                return true;
        }
        return false;
    }
    public static string[] vtol = new string[]
    {
        "42c_aim9ex1",
        "42c_aim9ex2",
        "agm89x1",
        "av42_gbu12x1",
        "av42_gbu12x2",
        "av42_gbu12x3",
        "cagm-6",
        "cbu97x1",
        "gau-8",
        "gbu38x1",
        "gbu38x2",
        "gbu38x3",
        "gbu39x3",
        "gbu39x4u",
        "h70-4x4",
        "h70-x19",
        "h70-x7",
        "hellfirex4",
        "iris-t-x1",
        "iris-t-x2",
        "iris-t-x3",
        "m230",
        "marmx1",
        "maverickx1",
        "maverickx3",
        "mk82HDx1",
        "mk82HDx2",
        "mk82HDx3",
        "mk82x1",
        "mk82x2",
        "mk82x3",
        "sidearmx1",
        "sidearmx2",
        "sidearmx3",
        "sidewinderx1",
        "sidewinderx2",
        "sidewinderx3",
    };
    public static string[] afighter = new string[]
    {
        "af_aim9",
        "af_amraam",
        "af_amraamRail",
        "af_amraamRailx2",
        "af_dropTank",
        "af_gun",
        "af_maverickx1",
        "af_maverickx3",
        "af_mk82",
        "af_tgp",
        "fa26-cft",
        "fa26_agm161",
        "fa26_agm89x1",
        "fa26_aim7",
        "fa26_aim7Rail",
        "fa26_aim9e",
        "fa26_aim9ex2",
        "fa26_aim9x2",
        "fa26_aim9x3",
        "fa26_cagm-6",
        "fa26_cbu97x1",
        "fa26_droptank",
        "fa26_droptankXL",
        "fa26_gbu12x1",
        "fa26_gbu12x2",
        "fa26_gbu12x3",
        "fa26_gbu38x1",
        "fa26_gbu38x2",
        "fa26_gbu38x3",
        "fa26_gbu39x4uFront",
        "fa26_gbu39x4uRear",
        "fa26_gun",
        "fa26_harmx1",
        "fa26_harmx1dpMount",
        "fa26_iris-t-x1",
        "fa26_iris-t-x2",
        "fa26_iris-t-x3",
        "fa26_maverickx1",
        "fa26_maverickx3",
        "fa26_mk82HDx1",
        "fa26_mk82HDx2",
        "fa26_mk82HDx3",
        "fa26_mk82x2",
        "fa26_mk82x3",
        "fa26_mk83x1",
        "fa26_sidearmx1",
        "fa26_sidearmx2",
        "fa26_sidearmx3",
        "fa26_tgp",
        "h70-x14ld",
        "h70-x14ld-under",
        "h70-x7ld",
        "h70-x7ld-under"
    };
    public static string[] f45a = new string[]
    {
        "f45-agm145I",
        "f45-agm145ISide",
        "f45-agm145x3",
        "f45-gbu39",
        "f45-gbu53",
        "f45_agm161",
        "f45_agm161Internal",
        "f45_aim9x1",
        "f45_amraamInternal",
        "f45_amraamRail",
        "f45_droptank",
        "f45_gbu12x1",
        "f45_gbu12x2Internal",
        "f45_gbu38x1",
        "f45_gbu38x2Internal",
        "f45_gbu38x4Internal",
        "f45_gun",
        "f45_mk82Internal",
        "f45_mk82x1",
        "f45_mk82x4Internal",
        "f45_mk83x1",
        "f45_mk83x1Internal",
        "f45_sidewinderx2"
    };
}
