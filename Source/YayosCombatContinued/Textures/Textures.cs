using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	[StaticConstructorOnStartup]
	internal class Textures
	{
		public static readonly Texture2D AmmoEject = ContentFinder<Texture2D>.Get("YCC_AmmoEject", true);
		public static readonly Texture2D AmmoReload = ContentFinder<Texture2D>.Get("YCC_AmmoReload", true);
	}
}
