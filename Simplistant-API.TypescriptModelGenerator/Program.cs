﻿using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Simplistant_API.Attributes;
using Simplistant_API.Controllers;
using Simplistant_API.DTO;

namespace Simplistant_API.TypescriptModelGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GenerateTypeDefinitions();
            GenerateAPIDefinitions();
        }

        private const string DTO_OUTPUT_FILE = "../../../../../Simplistant/dto.d.ts"; 
        private static void GenerateTypeDefinitions()
        {
            var enums = typeof(ResponseStatus).Assembly.GetTypes()
                .Where(x => x.IsEnum && x.Namespace?.Contains("DTO") == true)
                .OrderBy(x => x.Name)
                .Select(ToTypescriptEnum)
                .ToList();

            var dtos = typeof(MessageResponse).Assembly.GetTypes()
                .Where(x => x.IsClass && x.Namespace?.Contains("DTO") == true)
                .OrderBy(x => x.Name)
                .Select(ToTypescriptInterface)
                .ToList();

            var file_data = "//Auto-generated type definitions" + "\r\n\r\n"
                + string.Join("\r\n", enums) + "\r\n"
                + string.Join("\r\n", dtos);

            File.WriteAllText(DTO_OUTPUT_FILE, file_data);
        }

        private static string ToTypescriptEnum(Type type)
        {
            var enum_values = Enum.GetValues(type);
            var enum_names = new List<string>();
            for (var i = 0; i < enum_values.Length; i++)
            {
                enum_names.Add(enum_values.GetValue(i) + ",");
            }
            return $"export enum {type.Name} {{\r\n\t{string.Join("\r\n\t", enum_names)}\r\n}}\r\n";
        }

        private static string ToTypescriptInterface(Type type) =>
            $"export interface {type.Name} {{\r\n\t{string.Join("\r\n\t", type.GetProperties().Select(ToTypescriptProperty).ToList())}\r\n}}\r\n";

        private static string ToTypescriptProperty(PropertyInfo property) => $"{property.Name}: {ToTypescriptType(property.PropertyType)};";

        private static string ToTypescriptType(Type propType)
        {
            return propType.Name switch
            {
                "String" => "string",
                "Int32" => "number",
                "Boolean" => "boolean",
                _ => propType.IsGenericType
                    ? ToTypescriptType(propType.Name, propType.GenericTypeArguments)
                    : propType.Name,
            };
        }

        private static string ToTypescriptType(string propTypeName, Type[] genericTypeArguments)
        {
            if (genericTypeArguments.Length != 1)
            {
                return "any";
            }

            var genericType = genericTypeArguments[0];
            return propTypeName switch
            {
                "Nullable`1" => genericType.Name == "String" ? "string"
                    : genericType.Name == "Int32" ? "number"
                    : genericType.Name == "Boolean" ? "boolean"
                    : "any",
                "List`1" => genericType.Name == "String" ? "string[]"
                    : genericType.Name == "Int32" ? "number[]"
                    : genericType.Name == "Boolean" ? "boolean[]"
                    : "any",
                _ => "any"
            };
        }

        private const string API_OUTPUT_FILE = "../../../../../Simplistant/api.tsx";
        private static void GenerateAPIDefinitions()
        {
            var api_funcs = typeof(AccountController).Assembly.GetTypes()
                .Where(x => x.IsClass && x.GetCustomAttribute<ApiControllerAttribute>() != null)
                .Select(x => new
                {
                    x.Name,
                    Methods = x.GetMethods()
                        .Where(x => x.GetCustomAttribute<API_IgnoreAttribute>() == null
                            && (x.GetCustomAttribute<HttpGetAttribute>() != null 
                            || x.GetCustomAttribute<HttpPostAttribute>() != null))
                        .OrderBy(y => y.Name)
                        .ToList()
                }).OrderBy(x => x.Name)
                .SelectMany(x => x.Methods.Select(y => new
                {
                    ControllerName = x.Name,
                    Action = y
                }))
                .Select(x => ToTypescriptFunction(x.ControllerName, x.Action))
                .ToList();

            var file_data = "//Auto-generated client-side API functions\r\n"
                + "import axios, { AxiosError } from \"axios\"\r\n"
                + "import * as DTO from \"./dto\";\r\n\r\n"
                + "const api_uri = \"https://simplistant-api.azurewebsites.net\";\r\n\r\n"
                + string.Join("\r\n\r\n", api_funcs);

            File.WriteAllText(API_OUTPUT_FILE, file_data);
        }

        private static string ToTypescriptFunction(string controllerName, MethodInfo action)
        {
            var actionName = action.Name;
            var parameters = action.GetParameters();
            var parameterString = parameters.Length == 1
                ? $"{parameters[0].Name}: {ToTSType(parameters[0].ParameterType.Name)}"
                : "";
            var actionHttpType = action.GetCustomAttribute<HttpGetAttribute>() != null
                ? "get"
                : "post";
            var post_data = parameters.Length == 1 && actionHttpType == "post"
                ? $", {parameters[0].Name}"
                : "";
            var get_params = parameters.Length == 1 && actionHttpType == "get"
                ? $"?code=${{{parameters[0].Name}}}"
                : "";
            var actionReturnType = ToTSType(action.ReturnType.Name);

            return $@"export const {actionName} = async ({parameterString}) => {{
    const endpoint = `${{api_uri}}/{controllerName}/{actionName}{get_params}`;
    return await axios.{actionHttpType}<{actionReturnType}>(endpoint{post_data})
        .then(response => {{ return response.data }})
        .catch((axiosError: AxiosError) => {{
            if (axiosError.response!.status === 401) {{
                return 0;
            }} else {{
                return axiosError.message;
            }}
        }}).catch(_ => {{ return ""An unexpected error has occurred."" }});
}}";
        }

        private static object ToTSType(string returnTypeName) => returnTypeName switch
        {
            "String" => "string",
            "Int32" => "number",
            "Boolean" => "boolean",
            _ => $"DTO.{returnTypeName}"
        };
    }
}