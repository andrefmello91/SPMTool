﻿using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;

// As informações gerais sobre um assembly são controladas por
// conjunto de atributos. Altere estes valores de atributo para modificar as informações
// associada a um assembly.
[assembly: AssemblyTitle("SPMTool")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SPMTool")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Definir ComVisible como false torna os tipos neste assembly invisíveis
// para componentes COM. Caso precise acessar um tipo neste assembly de
// COM, defina o atributo ComVisible como true nesse tipo.
[assembly: ComVisible(false)]

// O GUID a seguir será destinado à ID de typelib se este projeto for exposto para COM
[assembly: Guid("25cfffc7-0ea2-4774-8125-f594d5e51e2a")]

// As informações da versão de um assembly consistem nos quatro valores a seguir:
//
//      Versão Principal
//      Versão Secundária 
//      Número da Versão
//      Revisão
//
// É possível especificar todos os valores ou usar como padrão os Números de Build e da Revisão
// usando o "*" como mostrado abaixo:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Assembly AutoCAD command class
[assembly: CommandClass(typeof(SPMTool.Commands.AcadCommands))]