using System;
using System.Threading.Tasks;

namespace NosCore.Travis.Runner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Function.TravisCheck(new InputObject
            {
                Build_Id = 637808857,
                Travis_Branch = "master",
                Travis_Commit = "ba21bd30589fd152126e13df30e0cc78ccdf2837",
                Travis_Pull_Request = false,
                Travis_Repo_Slug = "NosCoreIO/NosCore",
                Travis_Test_Result = 1,
            });
        }
    }
}
