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
                Build_Id = 639007564,
                Travis_Branch = "master",
                Travis_Commit = "ff8476e48e7cf31650f10e9f0bcfc6d66f534a22",
                Travis_Pull_Request = false,
                Travis_Repo_Slug = "NosCoreIO/NosCore",
                Travis_Test_Result = 1,
            });
        }
    }
}
