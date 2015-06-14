using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcScaf
{
    partial class ScaffTemplate
    {
        public TemplateTransformationContext TemplateTransformationContext { get; private set; }

        public ScaffTemplate(TemplateTransformationContext templateTransformationContext)
        {
            TemplateTransformationContext = templateTransformationContext;
        }

        public void NewLine()
        {
            Write(Environment.NewLine);
        }
    }
}
