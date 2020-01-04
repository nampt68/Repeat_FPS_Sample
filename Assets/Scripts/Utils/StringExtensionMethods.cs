using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Convenience functions for strings//이클래스는 전역이다.
public static class StringExtensionMethods
{//확장메소드이다. static class 이름, 함수를 static 반환형식 형식(this 대상형식, 매개변수 목록)
    public static string AfterLast(this string str, string sub)
    {
        var idx = str.LastIndexOf(sub);
        return idx < 0 ? "" : str.Substring(idx + sub.Length);
    }

    public static string BeforeLast(this string str, string sub)
    {
        var idx = str.LastIndexOf(sub);
        return idx < 0 ? "" : str.Substring(0, idx);
    }

    public static string AfterFirst(this string str, string sub)
    {
        var idx = str.IndexOf(sub);
        return idx < 0 ? "" : str.Substring(idx + sub.Length);
    }

    public static string BeforeFirst(this string str, string sub)
    {
        var idx = str.IndexOf(sub);
        return idx < 0 ? "" : str.Substring(0, idx);
    }

    public static int PrefixMatch(this string str, string prefix)
    {
        int l = 0, slen = str.Length, plen = prefix.Length;
        while(l<slen && l<plen)
        {
            if (str[l] != prefix[l])
                break;
            l++;
        }
        return l;
    }
}
