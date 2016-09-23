public class ProjectJsonUtil
{
    public static string ReadProjectJsonVersion(ICakeContext context)
    {
        var projects = context.GetFiles("./src/**/project.json");
        foreach (var project in projects)
        {
            return ReadProjectJsonVersion(project.FullPath);
        }
        throw new CakeException("Could not find any project.json files.");
    }

    public static string ReadProjectJsonVersion(string projectJsonPath)
    {
        var content = System.IO.File.ReadAllText(projectJsonPath, Encoding.UTF8);
        var node = Newtonsoft.Json.Linq.JObject.Parse(content);
        if (node["version"] != null)
        {
            var version = node["version"].ToString();
            return version.Replace("-*", "");
        }
        throw new CakeException("Could not parse version.");
    }

    public static bool PatchProjectJsonVersion(FilePath project, string version)
    {
        // var content = System.IO.File.ReadAllText(project.FullPath, Encoding.UTF8);
        // var node = Newtonsoft.Json.Linq.JObject.Parse(content);
        // if (node["version"] != null)
        // {
        //     node["version"].Replace(version);
        //     System.IO.File.WriteAllText(project.FullPath, node.ToString(), Encoding.UTF8);
        //     return true;
        // };
        // return false;

        bool versionFound = false;
        Newtonsoft.Json.Linq.JObject node;

        using (var file = new System.IO.FileStream(project.FullPath, FileMode.Open))
        using (var stream = new System.IO.StreamReader(file))
        using (var json = new Newtonsoft.Json.JsonTextReader(stream))
        {
            node = Newtonsoft.Json.Linq.JObject.Load(json);
        }

        var versionAttr = node.Property("version");
        if (versionAttr == null)
        {
            node.Add("version", new Newtonsoft.Json.Linq.JValue(version));
            versionFound = false;
        }
        else
        {
            versionAttr.Value = version;
            versionFound = true;
        }

        System.IO.File.WriteAllText(project.FullPath, node.ToString(), Encoding.UTF8);

        return versionFound;
    }
}
