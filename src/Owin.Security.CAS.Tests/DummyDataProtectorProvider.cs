using Microsoft.Owin.Security.DataProtection;

namespace Owin.Security.CAS.Tests
{
    //Dummy data protection provider to allow tests to run on Mono
    public class DummyDataProtectorProvider : IDataProtectionProvider
    {
        public IDataProtector Create(params string[] purposes)
        {
            return new DummyDataProtector();
        }

        private class DummyDataProtector : IDataProtector
        {
            public byte[] Protect(byte[] userData)
            {
                return userData;
            }

            public byte[] Unprotect(byte[] protectedData)
            {
                return protectedData;
            }
        }
    }
}
