using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace StoredProcScaf
{
    public class TypeHelper
    {
        private readonly DTE _dte;

        public TypeHelper(DTE dte)
        {
            _dte = dte;
        }

        public IEnumerable<string> FindSolutionTypes()
        {
           
            foreach (Project project in _dte.Solution.Projects)
            {
                yield return GetSolutionTypes(project.CodeModel.CodeElements);
            }
        }

        private string GetSolutionTypes(CodeElements codeElements)
        {
            foreach (object ce in codeElements)
            {
                CodeElement codeElement = (CodeElement) ce;
                if (codeElement is CodeNamespace || codeElement is CodeClass)
                {

                    var classElement = codeElement as CodeClass;
                    if (classElement != null && classElement.Access == vsCMAccess.vsCMAccessPublic)
                    {
                        GetSolutionTypes(classElement.Members);
                        return classElement.Name;
                    }

                    var codeNamespace = codeElement as CodeNamespace;
                    if (codeNamespace != null)
                    {
                        GetSolutionTypes(codeNamespace.Members);
                    }
                }
            }

            return string.Empty;
        } 
    }
}
