using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.Analysis))]

namespace SPMTool
{
    public partial class Analysis
    {
	    //public partial class Panel:SPMTool.Panel
	    //{
     //       public Panel(ObjectId panelObject) : base(panelObject)
     //       {
     //       }

     //       // Get the dimensions of surrounding stringers
     //       public static void StringersDimensions(Panel panel, Stringer[] stringers)
     //       {
	    //        // Initiate the stringer dimensions
	    //        double[] strDims = new double[4];

	    //        // Analyse panel grips
	    //        for (int i = 0; i < 4; i++)
	    //        {
		   //         int grip = panel.Grips[i];

		   //         // Verify if its an internal grip of a stringer
		   //         foreach (var stringer in stringers)
		   //         {
			  //          if (grip == stringer.Grips[1])
			  //          {
				 //           // The dimension is the half of stringer height
				 //           strDims[i] = 0.5 * stringer.Height;
				 //           break;
			  //          }
		   //         }
	    //        }

	    //        // Save to panel
	    //        panel.StringerDimensions = strDims;
     //       }

     //       public partial class NonLinear
     //       {
     //           //// Calculate panel initial nonlinear parameters
     //           //public static void InitialParameters(Panel[] panels, Stringer[] stringers)
     //           //{
     //           //    foreach (var panel in panels)
     //           //    {
     //           //        // Get surrounding stringers dimensions
     //           //        StringersDimensions(panel, stringers);

     //           //        // Calculate B*A and Q*P
     //           //        BAMatrix(panel);
     //           //        QPMatrix(panel);

     //           //        // Get the effective ratio off panel
     //           //        var (pxEf, pyEf) = panel.EffectiveRatio;

     //           //        // Calculate the initial membrane stiffness each int. point
     //           //        Membrane[] membranes = new Membrane[4];
     //           //        for (int i = 0; i < 4; i++)
     //           //            membranes[i] = Membrane.InitialStiffness(panel, (pxEf[i], pyEf[i]));

     //           //        // Set to panel
     //           //        panel.IntPointsMembrane = membranes;
     //           //    }
     //           //}
     //       }
     //   }
    }
}