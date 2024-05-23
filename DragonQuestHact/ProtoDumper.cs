using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace DQProtoGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"C:\Users\shalzuth\Downloads\Il2CppDumper-v6.4.19\DummyDll\Assembly-CSharp.dll";
            var assembly = AssemblyDefinition.ReadAssembly(path);
            var types = assembly.MainModule.GetTypes().ToList().FindAll(t => t.Namespace == "MQRPC");
            var sb = new StringBuilder();
            var messageNames = new List<String>();
            var enumVars = new List<String> { "Area" };
            Dictionary<string, string> shorthandMap = new Dictionary<string, string>
            {
                { "Boolean", "bool" },
                { "Int32", "int32" },
                { "UInt32", "uint32" },
                { "UInt64", "uint64" },
                { "String", "string" },
                { "Double", "double" },
                { "ByteString", "bytes" },
            };
            sb.AppendLine("syntax = \"proto3\";");
            sb.AppendLine("package MQRPC;");
            sb.AppendLine("message Empty { }");
            sb.AppendLine("message Timestamp {");
            sb.AppendLine("  int64 seconds = 1;");
            sb.AppendLine("  int32 nanos = 2;");
            sb.AppendLine("}");
            sb.AppendLine("enum OperationType {");
            sb.AppendLine("  None_OperationType = 0;");
            sb.AppendLine("  Create_OperationType = 1;");
            sb.AppendLine("  Update_OperationType = 2;");
            sb.AppendLine("  Delete_OperationType = 3;");
            sb.AppendLine("}");
            sb.AppendLine("enum RankingStatus {");
            sb.AppendLine("  Ok_RankingStatus = 0;");
            sb.AppendLine("  OutOfRange_RankingStatus = 1;");
            sb.AppendLine("  NotRanking_RankingStatus = 2;");
            sb.AppendLine("}");
            sb.AppendLine("enum NewsCategory {");
            sb.AppendLine("  Unknown_NewsCategory = 0;");
            sb.AppendLine("  Important_NewsCategory = 1;");
            sb.AppendLine("  Renewal_NewsCategory = 2;");
            sb.AppendLine("  Expiration_NewsCategory = 3;");
            sb.AppendLine("  Maintenance_NewsCategory = 4;");
            sb.AppendLine("  Recover_NewsCategory = 5;");
            sb.AppendLine("  Campaign_NewsCategory = 6;");
            sb.AppendLine("  Incident_NewsCategory = 7;");
            sb.AppendLine("  Event_NewsCategory = 8;");
            sb.AppendLine("  Infomation_NewsCategory = 9;");
            sb.AppendLine("  Others_NewsCategory = 10;");
            sb.AppendLine("}");
            sb.AppendLine("enum JewelPurchaseStatus {");
            sb.AppendLine("  Success_JewelPurchaseStatus = 0;");
            sb.AppendLine("  Failure_JewelPurchaseStatus = 1;");
            sb.AppendLine("  Pending_JewelPurchaseStatus = 2;");
            sb.AppendLine("  AlreadyProcessed_JewelPurchaseStatus = 3;");
            sb.AppendLine("  PurchaseExceeded_JewelPurchaseStatus = 4;");
            sb.AppendLine("  FraudulentRefundsUser_JewelPurchaseStatus = 5;");
            sb.AppendLine("  FraudulentReceipt_JewelPurchaseStatus = 6;");
            sb.AppendLine("}");
            sb.AppendLine("enum AcquisitionDirection {");
            sb.AppendLine("  Success_AcquisitionDirection = 0;");
            sb.AppendLine("  Failure_AcquisitionDirection = 1;");
            sb.AppendLine("}");
            sb.AppendLine("enum DefeatReasonType {");
            sb.AppendLine("  Unset_DefeatReasonType = 0;");
            sb.AppendLine("  Annihilation_DefeatReasonType = 1;");
            sb.AppendLine("  Timeout_DefeatReasonType = 2;");
            sb.AppendLine("  Retire_DefeatReasonType = 3;");
            sb.AppendLine("}");
            sb.AppendLine("enum Status {");
            sb.AppendLine("  Status1 = 0;");
            sb.AppendLine("  Status2 = 1;");
            sb.AppendLine("  Status3 = 2;");
            sb.AppendLine("  Status4 = 3;");
            sb.AppendLine("}");
            foreach (var t in types)
            {
                //if (!t.FullName.Contains("Entry")) continue;
                //Console.WriteLine(t.FullName + " : " + t.Interfaces.Count);
                if (t.HasNestedTypes)
                {
                    foreach (var nt in t.NestedTypes)
                    {
                        if (nt.HasMethods && nt.BaseType.FullName.Contains("Grpc.Core.ClientBase"))
                        {
                            sb.AppendLine("service " + t.Name + " {");

                            foreach (var m in nt.Methods)
                            {
                                if (m.HasParameters && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.Name == "CallOptions" && !m.Name.Contains("Async"))
                                {
                                    sb.AppendLine("  rpc " + m.Name + " (" + m.Parameters[0].ParameterType.Name + ") returns (" + m.ReturnType.Name + ") {}");
                                }
                            }
                            sb.AppendLine("}");
                        }
                        if (nt.HasNestedTypes)
                        {
                            foreach (var ntt in nt.NestedTypes)
                            {
                                if (ntt.Properties.Count == 0)
                                {
                                    if (ntt.Name == "Status") continue;
                                    sb.AppendLine("enum " + ntt.Name + " {");

                                    foreach (var p in ntt.Fields) if (p.HasConstant)
                                        {
                                            var pName = p.Name;
                                            if (enumVars.Contains(pName))
                                                pName = p.Name + "_" + ntt.Name;
                                            enumVars.Add(pName);
                                            sb.AppendLine("  " + pName + " = " + p.Constant + ";");
                                        }
                                }
                                else
                                {
                                    if (messageNames.Contains(ntt.Name)) continue;
                                    messageNames.Add(ntt.Name);
                                    sb.AppendLine("message " + ntt.Name + " {");
                                    foreach (var p in ntt.Properties)
                                    {
                                        if (p.Name == "Descriptor" || p.Name == "Parser" || p.Name == "pb::Google.Protobuf.IMessage.Descriptor") continue;
                                        var pp = ntt.Fields.FirstOrDefault(f => f.Name.ToLower() == p.Name.ToLower() + "fieldnumber");
                                        if (pp == null) continue;
                                        if (p.PropertyType is GenericInstanceType genType)
                                            sb.AppendLine("  repeated " + (shorthandMap.ContainsKey(genType.GenericArguments[0].Name) ? shorthandMap[genType.GenericArguments[0].Name] : genType.GenericArguments[0].Name) + " " + p.Name + " = " + pp.Constant + ";");
                                        else
                                            sb.AppendLine("  " + (shorthandMap.ContainsKey(p.PropertyType.Name) ? shorthandMap[p.PropertyType.Name] : p.PropertyType.Name) + " " + p.Name + " = " + pp.Constant + ";");
                                    }
                                }
                                sb.AppendLine("}");

                            }
                        }

                    }
                }
                if (t.Interfaces.Count == 0 || messageNames.Contains(t.Name)) continue;
                messageNames.Add(t.Name);
                sb.AppendLine("message " + t.Name + " {");
                foreach (var p in t.Properties)
                {
                    if (p.Name == "Descriptor" || p.Name == "Parser" || p.Name == "pb::Google.Protobuf.IMessage.Descriptor") continue;
                    var pp = t.Fields.FirstOrDefault(f => f.Name.ToLower() == p.Name.ToLower() + "fieldnumber");
                    if (pp == null) continue;
                    if (p.PropertyType is GenericInstanceType genType)
                        sb.AppendLine("  repeated " + (shorthandMap.ContainsKey(genType.GenericArguments[0].Name) ? shorthandMap[genType.GenericArguments[0].Name] : genType.GenericArguments[0].Name) + " " + p.Name + " = " + pp.Constant + ";");
                    else
                        sb.AppendLine("  " + (shorthandMap.ContainsKey(p.PropertyType.Name) ? shorthandMap[p.PropertyType.Name] : p.PropertyType.Name) + " " + p.Name + " = " + pp.Constant + ";");
                }
                sb.AppendLine("}");
            }
            Console.WriteLine(sb.ToString());
            Console.Read();
        }
    }
}
