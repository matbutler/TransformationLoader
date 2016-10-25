using NUnit.Framework;
using System;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Enums;
using TransformationCore.Exceptions;

namespace Transformation.Tests.Loader
{
    [TestFixture]
    [Category("TransformationFilter")]
    public class TransformationFilterTest
    {
        private static TransformationFilter BuildTransformationFilter(string configXML)
        {
            var config = XElement.Parse(configXML);

            return new TransformationFilter(config);
        }

        [Test]
        [TestCase("<filter />")]
        [TestCase("<filter field='test'/>")]
        [TestCase("<filter field='test' operator='equal'/>")]
        public void TransformationFilter_CTOR_MissingConfig_ThrowsException(string configXML)
        {
            var exception = Assert.Catch(() => BuildTransformationFilter(configXML));

            Assert.That(exception, Is.InstanceOf<TransformationFilterException>());
            Assert.That(exception.Message, Does.Contain("Invalid Filter"));
        }

        [Test]
        public void TransformationFilter_CTOR_InvalidOperator_ThrowsException()
        {
            var exception = Assert.Catch(() => BuildTransformationFilter("<filter field='test' operator='test' value='' />"));

            Assert.That(exception, Is.InstanceOf<TransformationFilterException>());
            Assert.That(exception.Message, Does.Contain("Invalid operator"));
        }

        [Test]
        [TestCase(1, TransformationFilterOperatorEnum.LessThan, false)]
        [TestCase(1, TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase(1, TransformationFilterOperatorEnum.Equal, true)]
        [TestCase(1, TransformationFilterOperatorEnum.GreaterThanEqual, true)]
        [TestCase(1, TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase(1, TransformationFilterOperatorEnum.NotEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase(5, TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.Equal, false)]
        [TestCase(5, TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase(5, TransformationFilterOperatorEnum.NotEqual, true)]
        [TestCase(10, TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase(10, TransformationFilterOperatorEnum.LessThanEqual, true)]
        [TestCase(10, TransformationFilterOperatorEnum.Equal, true)]
        [TestCase(10, TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase(10, TransformationFilterOperatorEnum.GreaterThan, false)]
        [TestCase(10, TransformationFilterOperatorEnum.NotEqual, false)]
        public void TransformationFilter_Integer_Test(long testData, TransformationFilterOperatorEnum operation, bool expectedResult)
        {
            var filter = BuildTransformationFilter(string.Format("<filter field='test' filtertype='INT' operator='{0}' value='5' />", operation));

            var result = filter.Check(testData);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(1, TransformationFilterOperatorEnum.LessThan, false)]
        [TestCase(1, TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase(1, TransformationFilterOperatorEnum.Equal, true)]
        [TestCase(1, TransformationFilterOperatorEnum.GreaterThanEqual, true)]
        [TestCase(1, TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase(1, TransformationFilterOperatorEnum.NotEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase(5, TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.Equal, false)]
        [TestCase(5, TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase(5, TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase(5, TransformationFilterOperatorEnum.NotEqual, true)]
        [TestCase(10, TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase(10, TransformationFilterOperatorEnum.LessThanEqual, true)]
        [TestCase(10, TransformationFilterOperatorEnum.Equal, true)]
        [TestCase(10, TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase(10, TransformationFilterOperatorEnum.GreaterThan, false)]
        [TestCase(10, TransformationFilterOperatorEnum.NotEqual, false)]
        public void TransformationFilter_Date_Test(int month, TransformationFilterOperatorEnum operation, bool expectedResult)
        {
            var testData = new DateTime(2016, month, 1);

            var filter = BuildTransformationFilter(string.Format("<filter field='test' filtertype='DATETIME' operator='{0}' value='2016-05-01' format='yyyy-MM-dd' />", operation));

            var result = filter.Check(testData);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("a", TransformationFilterOperatorEnum.LessThan, false)]
        [TestCase("a", TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase("a", TransformationFilterOperatorEnum.Equal, true)]
        [TestCase("a", TransformationFilterOperatorEnum.GreaterThanEqual, true)]
        [TestCase("a", TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase("a", TransformationFilterOperatorEnum.NotEqual, false)]
        [TestCase("c", TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase("c", TransformationFilterOperatorEnum.LessThanEqual, false)]
        [TestCase("c", TransformationFilterOperatorEnum.Equal, false)]
        [TestCase("c", TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase("c", TransformationFilterOperatorEnum.GreaterThan, true)]
        [TestCase("c", TransformationFilterOperatorEnum.NotEqual, true)]
        [TestCase("d", TransformationFilterOperatorEnum.LessThan, true)]
        [TestCase("d", TransformationFilterOperatorEnum.LessThanEqual, true)]
        [TestCase("d", TransformationFilterOperatorEnum.Equal, true)]
        [TestCase("d", TransformationFilterOperatorEnum.GreaterThanEqual, false)]
        [TestCase("d", TransformationFilterOperatorEnum.GreaterThan, false)]
        [TestCase("d", TransformationFilterOperatorEnum.NotEqual, false)]
        public void TransformationFilter_String_Test(string testData, TransformationFilterOperatorEnum operation, bool expectedResult)
        {

            var filter = BuildTransformationFilter(string.Format("<filter field='test' filtertype='STRING' operator='{0}' value='c' />", operation));

            var result = filter.Check(testData);

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        //[Test]
        //[TestCase("a", TransformationFilterOperatorEnum.Equal, "STRING", true)]
        //[TestCase("", TransformationFilterOperatorEnum.Equal, "STRING", false)]
        //[TestCase(null, TransformationFilterOperatorEnum.Equal, "STRING", false)]
        //public void TransformationFilter_Null_Test(string testData, TransformationFilterOperatorEnum operation, string filterType, bool expectedResult)
        //{
        //    var filter = BuildTransformationFilter(string.Format("<filter field='test' filtertype='{1}' operator='{0}' value='NULL' />", operation, filterType));

        //    var result = filter.Check(testData);

        //    Assert.That(result, Is.EqualTo(expectedResult));
        //}
    }
}
