using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Dynamic;


namespace CandidSDK
{
    internal class DynamicObjectReader : System.Dynamic.DynamicObject
    {
        public enum StringSearchOption
        {
            StartsWith,
            Contains,
            EndsWith
        }

        // Store the path to the file and the initial line count value.
        private string p_filePath;

        // Verify that file exists and store the path in the private variable.
        public void ReadOnlyFile(string filePath)
        {
            // Trap exception that occurs in reading the file.
            if (!File.Exists(filePath))
            {
                throw new Exception("File path does not exist.");
            }

            p_filePath = filePath;
        }
        // The GetPropertyValue method takes search criteria as input and returns the lines from a text file 
        // that match the search criteria.The dynamic methods provided by the ReadOnlyFile class call
        // the GetPropertyValue method to retrieve the corresponding results.
        public List<string> GetPropertyValue(string propertyName,
                                     StringSearchOption StringSearchOption = StringSearchOption.StartsWith,
                                     bool trimSpaces = true)
        {
            StreamReader sr = null;
            List<string> results = new List<string>();
            string line = "";
            string testLine = "";

            try
            {
                sr = new StreamReader(p_filePath);

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();

                    // Perform a case-insensitive search by using the specified search options.
                    testLine = line.ToUpper();
                    if (trimSpaces) { testLine = testLine.Trim(); }

                    switch (StringSearchOption)
                    {
                        case StringSearchOption.StartsWith:
                            if (testLine.StartsWith(propertyName.ToUpper())) { results.Add(line); }
                            break;
                        case StringSearchOption.Contains:
                            if (testLine.Contains(propertyName.ToUpper())) { results.Add(line); }
                            break;
                        case StringSearchOption.EndsWith:
                            if (testLine.EndsWith(propertyName.ToUpper())) { results.Add(line); }
                            break;
                    }
                }
            }
            catch
            {
                // Trap any exception that occurs in reading the file and return null.
                results = null;
            }
            finally
            {
                if (sr != null) { sr.Close(); }
            }

            return results;
        }

        // Implement the TryGetMember method of the DynamicObject class for dynamic member calls.
        // The binder argument contains information about the referenced member, and the result argument refers to the result returned for 
        // the specified member.The TryGetMember method returns a boolean value that returns true if the requested member exists.Otherwise, return false.
        public override bool TryGetMember(GetMemberBinder binder,
                                          out object result)
        {
            result = GetPropertyValue(binder.Name);
            return result == null ? false : true;
        }

        // Implement the TryInvokeMember method of the DynamicObject class for
        // dynamic member calls that have arguments.
        // TryInvokeMember expects the first argument to be a value from the StringSearchOption 
        // enumerator that was defined in a previous step.The TryInvokeMember method expects the 
        // second argument to be a boolean value.If one or both of the arguments are valid values, 
        // they are passed to the GetPropertyValue method to retrieve the results.
        public override bool TryInvokeMember(InvokeMemberBinder binder,
                                             object[] args,
                                             out object result)
        {
            StringSearchOption StringSearchOption = StringSearchOption.StartsWith;
            bool trimSpaces = true;

            try
            {
                if (args.Length > 0) { StringSearchOption = (StringSearchOption)args[0]; }
            }
            catch
            {
                throw new ArgumentException("StringSearchOption argument must be a StringSearchOption enum value.");
            }

            try
            {
                if (args.Length > 1) { trimSpaces = (bool)args[1]; }
            }
            catch
            {
                throw new ArgumentException("trimSpaces argument must be a Boolean value.");
            }

            result = GetPropertyValue(binder.Name, StringSearchOption, trimSpaces);

            return result == null ? false : true;
        }
        private void Analyze()
        {
            //Unbounded integral number types(nat, int).
            //Bounded integral number(nat8, nat16, nat32, nat64, int8, int16, int32, int64).
            //Floating point types(float32, float64).
            //The Boolean type(bool).
            //Types for textual(text) and binary(blob) data.
            //Container types, including variants(opt, vec, record, variant).
            //Reference types(service, func, principal).
            //The special null, reserved and empty types.


        }
    }
}
