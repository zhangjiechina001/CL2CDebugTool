namespace CL2CDebugTool.Tests
{
    [TestClass]
    public class CL2CControllerTest
    {
        [TestMethod]
        public void TestGetBit()
        {
            ushort num1 = 0b01;
            Assert.IsTrue(CL2CController.GetBit(num1, 1));

            ushort num2 = 4;
            Assert.IsTrue(CL2CController.GetBit(num2, 3));
            Assert.IsFalse(CL2CController.GetBit(num2, 2));
        }

        [TestMethod]
        public void TestGetNum()
        {
            bool[] bArr = { false,true };
            Assert.IsTrue(CL2CController.GetNum(bArr)==2);

            bool[] bArr1 = { false,true, true  };
            Assert.IsTrue(CL2CController.GetNum(bArr1) == 6);
        }
    }
}