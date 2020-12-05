using QueenOfDreamer.API.Interfaces.Services;

namespace QueenOfDreamer.API.Services
{
    public static class ServicesFactory
    {
        public static ICommonServices GetCommonServices()
        {
            return new CommonServices();
        }

        public static IQueenOfDreamerServices GetPackageServices()
        {
            return new QueenOfDreamerServices();
        }
    }

}
