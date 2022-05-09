using System.Reflection;
using System.Reflection.Emit;

namespace MiCake.Core.Util.Reflection
{
    public static class ILGeneratorExtension
    {
        /// <summary>
        /// Emits the optimal IL to push the actual parameter values on the stack (<see cref="OpCodes.Ldarg_0"/>... <see cref="OpCodes.Ldarg"/>).
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="startAtArgument0">False to skip the very first argument: for a method instance Arg0 is the 'this' object (see <see cref="System.Reflection.CallingConventions"/>) HasThis and ExplicitThis).</param>
        /// <param name="count">Number of parameters to push.</param>
        public static void PushParameters(this ILGenerator g, bool startAtArgument0, int count)
        {
            if (count <= 0) return;

            int index = startAtArgument0 ? 0 : 1;

            while (count > 0)
            {
                LdArg(g, index);

                index++;
                count--;
            }
        }

        /// <summary>
        /// Emits the IL to push (<see cref="OpCodes.Ldarg"/>) the actual argument at the given index onto the stack.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="i">Parameter index (0 being the 'this' for instance method).</param>
        public static void LdArg(this ILGenerator g, int i)
        {
            if (i == 0) g.Emit(OpCodes.Ldarg_0);
            else if (i == 1) g.Emit(OpCodes.Ldarg_1);
            else if (i == 2) g.Emit(OpCodes.Ldarg_2);
            else if (i == 3) g.Emit(OpCodes.Ldarg_3);
            else if (i < 255) g.Emit(OpCodes.Ldarg_S, (byte)i);
            else g.Emit(OpCodes.Ldarg, (short)i);
        }



        /// <summary>
        /// Emits the IL to push (<see cref="OpCodes.Ldloc"/>) the given local on top of the stack.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="local">The local variable to push.</param>
        public static void LdLoc(this ILGenerator g, LocalBuilder local)
        {
            int i = local.LocalIndex;
            if (i == 0) g.Emit(OpCodes.Ldloc_0);
            else if (i == 1) g.Emit(OpCodes.Ldloc_1);
            else if (i == 2) g.Emit(OpCodes.Ldloc_2);
            else if (i == 3) g.Emit(OpCodes.Ldloc_3);
            else if (i < 255) g.Emit(OpCodes.Ldloc_S, (byte)i);
            else g.Emit(OpCodes.Ldloc, (short)i);
        }

        /// <summary>
        /// Emits the IL to pop (<see cref="OpCodes.Stloc"/>) the top of the stack into a local variable.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="local">The local variable to pop.</param>
        public static void StLoc(this ILGenerator g, LocalBuilder local)
        {
            int i = local.LocalIndex;
            if (i == 0) g.Emit(OpCodes.Stloc_0);
            else if (i == 1) g.Emit(OpCodes.Stloc_1);
            else if (i == 2) g.Emit(OpCodes.Stloc_2);
            else if (i == 3) g.Emit(OpCodes.Stloc_3);
            else if (i < 255) g.Emit(OpCodes.Stloc_S, (byte)i);
            else g.Emit(OpCodes.Stloc, (short)i);
        }

        /// <summary>
        /// Emits the IL to push the integer (emits the best opcode depending on the value: <see cref="OpCodes.Ldc_I4_0"/> 
        /// or <see cref="OpCodes.Ldc_I4_M1"/> for instance) value onto the stack.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="i">The integer value to push.</param>
        public static void LdInt32(this ILGenerator g, int i)
        {
            if (i == 0) g.Emit(OpCodes.Ldc_I4_0);
            else if (i == 1) g.Emit(OpCodes.Ldc_I4_1);
            else if (i == 2) g.Emit(OpCodes.Ldc_I4_2);
            else if (i == 3) g.Emit(OpCodes.Ldc_I4_3);
            else if (i == 4) g.Emit(OpCodes.Ldc_I4_4);
            else if (i == 5) g.Emit(OpCodes.Ldc_I4_5);
            else if (i == 6) g.Emit(OpCodes.Ldc_I4_6);
            else if (i == 7) g.Emit(OpCodes.Ldc_I4_7);
            else if (i == 8) g.Emit(OpCodes.Ldc_I4_8);
            else if (i == -1) g.Emit(OpCodes.Ldc_I4_M1);
            else if (i >= -128 && i <= 127) g.Emit(OpCodes.Ldc_I4_S, (byte)i);
            else g.Emit(OpCodes.Ldc_I4, i);
        }

        /// <summary>
        /// Emits the IL to pop (<see cref="OpCodes.Starg"/>) the top of the stack into the actual argument at the given index.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="i">Parameter index (0 being the 'this' for instance method).</param>
        public static void StArg(this ILGenerator g, int i)
        {
            if (i < 255) g.Emit(OpCodes.Starg_S, (byte)i);
            else g.Emit(OpCodes.Starg, (short)i);
        }

        /// <summary>
        /// Emits the IL to pop the top of the stack into the actual argument at the given instance.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="argsIndex">Parameter index (0 being the 'this' for instance method).</param>
        /// <param name="type">Instance type</param>
        public static void PushInstance(this ILGenerator g, int argsIndex, Type type)
        {
            g.Emit(OpCodes.Ldarg, argsIndex);
            if (type.IsValueType)
            {
                g.Emit(OpCodes.Unbox, type);
            }
            else
            {
                g.Emit(OpCodes.Castclass, type);
            }
        }

        /// <summary>
        ///  Emits the IL to pop (<see cref="OpCodes.Ldelem_Ref"/>) the top of the stack into the actual argument at the given array instance.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="argsIndex">Parameter index (0 being the 'this' for instance method).</param>
        /// <param name="arrayIndex">Array index</param>
        public static void PushArrayInstance(this ILGenerator g, int argsIndex, int arrayIndex)
        {
            g.Emit(OpCodes.Ldarg, argsIndex);
            g.Emit(OpCodes.Ldc_I4, arrayIndex);
            g.Emit(OpCodes.Ldelem_Ref);
        }

        /// <summary>
        /// Call the method(<see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/>)
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="methodInfo"></param>
        public static void CallMethod(this ILGenerator g, MethodInfo methodInfo)
        {
            if (methodInfo.IsFinal || !methodInfo.IsVirtual)
            {
                g.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                g.Emit(OpCodes.Callvirt, methodInfo);
            }
        }

        /// <summary>
        /// Return current method(<see cref="OpCodes.Ret"/>)
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        public static void Return(this ILGenerator g)
        {
            g.Emit(OpCodes.Ret);
        }
    }
}
