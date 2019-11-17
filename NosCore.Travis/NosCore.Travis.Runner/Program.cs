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
                Build_Id = 612970495,
                Travis_Branch = "master",
                Travis_Commit = "caba3d09527a6edff3564a1fa0662b4ed9458c10",
                Travis_Pull_Request = false,
                Travis_Repo_Slug = "NosCoreIO/NosCore",
                Travis_Test_Result = 1,
            });
        }
    }
}
