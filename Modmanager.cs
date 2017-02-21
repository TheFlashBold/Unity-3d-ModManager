using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Modmanager : MonoBehaviour {
        
    Dictionary<string, MethodInfo> updatemod = new Dictionary<string, MethodInfo>();
    Dictionary<string, object> instancemod = new Dictionary<string, object>();
    Dictionary<string, System.Type> typemod = new Dictionary<string, System.Type>();

    bool m_isLoadingMod = false;
    WWW m_www;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    void Start ()
    {
        StartCoroutine(LoadMod(@"file:///E:\Work\Unity\ShittyTest\TestMod\TestMod\bin\Debug\TestMod.dll"));
        string[] filenames;
        filenames = Directory.GetFiles(Environment.CurrentDirectory, "*.*", SearchOption.TopDirectoryOnly);
        File.WriteAllText("test.txt", filenames.ToString());
    }
	
	void Update ()
    {
		foreach(KeyValuePair<string, MethodInfo> modUpdate in updatemod)
        {
            modUpdate.Value.Invoke(instancemod[modUpdate.Key], null);
        }
	}

    IEnumerator LoadMod(string path)
    {
        if (!m_isLoadingMod)
        {

            m_www = new WWW(path);

            while (!m_www.isDone)
            {
                yield return null;
            }

            Assembly assembly = LoadAssembly();
            if (assembly != null)
            {
                OnAssemblyLoaded(new WWWAssembly(m_www.url, assembly));
            }
        }
        else
        {
            Debug.Log("Modmanager is already trying to load a mod!");
        }

        yield return null;
    }

    private Assembly LoadAssembly()
    {
        try
        {
            return Assembly.Load(m_www.bytes);
        }
        catch (System.Exception e)
        {
            Debug.Log("ERROR: Loading Mod " + m_www.url + "\n" + e.ToString());
            return null;
        }
    }

    void OnAssemblyLoaded(WWWAssembly loadedAssembly)
    {
        System.Type type = loadedAssembly.Assembly.GetType("TestMod.Main");

        object instance = loadedAssembly.Assembly.CreateInstance("TestMod.Main");
        
        FieldInfo fieldmodName = type.GetField("ModName");
        string modName = (fieldmodName.GetValue(null) as string);

        FieldInfo fieldmodVersion = type.GetField("ModVersion");
        string modVersion = (fieldmodVersion.GetValue(null) as string);

        Debug.Log("Successfully loaded " + modName + " V" + modVersion);        

        typemod.Add(modName, type);
        instancemod.Add(modName, instance);
        
        MethodInfo start = type.GetMethod("Start");
        start.Invoke(instance, null);

        updatemod.Add(modName, type.GetMethod("Update"));
        m_isLoadingMod = false;
    }
}

public class WWWAssembly
{
    private string m_URL;
    private Assembly m_Assembly;

    public string URL
    {
        get
        {
            return m_URL;
        }
    }

    public Assembly Assembly
    {
        get
        {
            return m_Assembly;
        }
    }

    public WWWAssembly(string url, Assembly assembly)
    {
        m_URL = url;
        m_Assembly = assembly;
    }
}