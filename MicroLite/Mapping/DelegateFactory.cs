﻿// -----------------------------------------------------------------------
// <copyright file="DelegateFactory.cs" company="Project Contributors">
// Copyright Project Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using MicroLite.TypeConverters;

namespace MicroLite.Mapping
{
    internal static class DelegateFactory
    {
        private static readonly MethodInfo s_convertFromDbValueMethod = typeof(ITypeConverter).GetMethod("ConvertFromDbValue", new[] { typeof(IDataReader), typeof(int), typeof(Type) });
        private static readonly MethodInfo s_convertToDbValueMethod = typeof(ITypeConverter).GetMethod("ConvertToDbValue", new[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo s_dataRecordGetFieldCount = typeof(IDataRecord).GetProperty("FieldCount").GetGetMethod();
        private static readonly MethodInfo s_dataRecordGetName = typeof(IDataRecord).GetMethod("GetName");
        private static readonly MethodInfo s_dataRecordIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        private static readonly ConstructorInfo s_sqlArgumentConstructor = typeof(SqlArgument).GetConstructor(new[] { typeof(object), typeof(DbType) });
        private static readonly MethodInfo s_stringEquals = typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(string) });
        private static readonly MethodInfo s_typeConverterDefaultMethod = typeof(TypeConverter).GetProperty("Default").GetGetMethod();
        private static readonly MethodInfo s_typeConverterForMethod = typeof(TypeConverter).GetMethod("For", new[] { typeof(Type) });
        private static readonly MethodInfo s_typeGetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");

        internal static Func<object, object> CreateGetIdentifier(IObjectInfo objectInfo)
        {
            var dynamicMethod = new DynamicMethod(
                name: "MicroLite" + objectInfo.ForType.Name + "GetIdentifier",
                returnType: typeof(object),
                parameterTypes: new[] { typeof(object) }); //// arg_0

            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            //// var instance = ({Type})arg_0;
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Castclass, objectInfo.ForType);

            //// var identifier = instance.Id;
            ilGenerator.Emit(OpCodes.Callvirt, objectInfo.TableInfo.IdentifierColumn.PropertyInfo.GetGetMethod());

            //// value = (object)identifier;
            ilGenerator.EmitBoxIfValueType(objectInfo.TableInfo.IdentifierColumn.PropertyInfo.PropertyType);

            //// return identifier;
            ilGenerator.Emit(OpCodes.Ret);

            var getIdentifierValue = (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));

            return getIdentifierValue;
        }

        internal static Func<object, SqlArgument[]> CreateGetInsertValues(IObjectInfo objectInfo)
        {
            var dynamicMethod = new DynamicMethod(
                name: "MicroLite" + objectInfo.ForType.Name + "GetInsertValues",
                returnType: typeof(SqlArgument[]),
                parameterTypes: new[] { typeof(object) }, //// arg_0
                m: typeof(ObjectInfo).Module);

            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.DeclareLocal(objectInfo.ForType);     //// loc_0 - {Type} instance;
            ilGenerator.DeclareLocal(typeof(SqlArgument[]));  //// loc_1 - SqlArgument[] sqlArguments;

            //// instance = ({Type})arg_0;
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Castclass, objectInfo.ForType);
            ilGenerator.Emit(OpCodes.Stloc_0);

            //// sqlArguments = new SqlArgument[count];
            ilGenerator.EmitEfficientInt(objectInfo.TableInfo.InsertColumnCount);
            ilGenerator.Emit(OpCodes.Newarr, typeof(SqlArgument));
            ilGenerator.Emit(OpCodes.Stloc_1);

            EmitGetPropertyValues(ilGenerator, objectInfo, c => c.AllowInsert);

            //// return sqlArguments;
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ret);

            var getInsertValues = (Func<object, SqlArgument[]>)dynamicMethod.CreateDelegate(typeof(Func<object, SqlArgument[]>));

            return getInsertValues;
        }

        internal static Func<object, SqlArgument[]> CreateGetUpdateValues(IObjectInfo objectInfo)
        {
            var dynamicMethod = new DynamicMethod(
                name: "MicroLite" + objectInfo.ForType.Name + "GetUpdateValues",
                returnType: typeof(SqlArgument[]),
                parameterTypes: new[] { typeof(object) }, //// arg_0
                m: typeof(ObjectInfo).Module);

            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.DeclareLocal(objectInfo.ForType);     //// loc_0 - {Type} instance;
            ilGenerator.DeclareLocal(typeof(SqlArgument[]));  //// loc_1 - SqlArgument[] sqlArguments;

            //// instance = ({Type})arg_0;
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Castclass, objectInfo.ForType);
            ilGenerator.Emit(OpCodes.Stloc_0);

            //// sqlArguments = new SqlArgument[count + 1]; //// Add 1 for the identifier
            ilGenerator.EmitEfficientInt(objectInfo.TableInfo.UpdateColumnCount + 1);
            ilGenerator.Emit(OpCodes.Newarr, typeof(SqlArgument));
            ilGenerator.Emit(OpCodes.Stloc_1);

            EmitGetPropertyValues(ilGenerator, objectInfo, c => c.AllowUpdate);

            //// sqlArguments[values.Length - 1] = entity.{Id};
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitEfficientInt(objectInfo.TableInfo.UpdateColumnCount);
            ilGenerator.Emit(OpCodes.Ldelema, typeof(SqlArgument));
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Callvirt, objectInfo.TableInfo.IdentifierColumn.PropertyInfo.GetGetMethod());

            ilGenerator.EmitBoxIfValueType(objectInfo.TableInfo.IdentifierColumn.PropertyInfo.PropertyType);

            ilGenerator.EmitEfficientInt((int)objectInfo.TableInfo.IdentifierColumn.DbType);
            ilGenerator.Emit(OpCodes.Newobj, s_sqlArgumentConstructor);

            //// sqlArguments[i] = new SqlArgument(value, column.DbType); OR sqlArguments[i] = new SqlArgument(converted, column.DbType);
            ilGenerator.Emit(OpCodes.Stobj, typeof(SqlArgument));

            //// return sqlArguments;
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ret);

            var getUpdateValues = (Func<object, SqlArgument[]>)dynamicMethod.CreateDelegate(typeof(Func<object, SqlArgument[]>));

            return getUpdateValues;
        }

        internal static Func<IDataReader, object> CreateInstanceFactory(IObjectInfo objectInfo)
        {
            var dynamicMethod = new DynamicMethod(
                name: "MicroLite" + objectInfo.ForType.Name + "Factory",
                returnType: typeof(object),
                parameterTypes: new[] { typeof(IDataReader) }, //// arg_0
                m: typeof(ObjectInfo).Module);

            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.DeclareLocal(objectInfo.ForType);     //// loc_0 - {Type} instance;
            ilGenerator.DeclareLocal(typeof(int));            //// loc_1 - int i
            ilGenerator.DeclareLocal(typeof(string));         //// loc_2 - string columnName

            Label isDBNull = ilGenerator.DefineLabel();
            Label getColumnName = ilGenerator.DefineLabel();
            var columnLabels = new Label[objectInfo.TableInfo.Columns.Count];
            Label incrementIndex = ilGenerator.DefineLabel();
            Label getFieldCount = ilGenerator.DefineLabel();
            Label returnEntity = ilGenerator.DefineLabel();

            //// var entity = new T();
            ilGenerator.Emit(OpCodes.Newobj, objectInfo.ForType.GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Stloc_0);

            //// var i = 0;
            ilGenerator.EmitEfficientInt(0);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Br, getFieldCount);

            //// if (dataReader.IsDBNull(i)) { continue; }
            ilGenerator.MarkLabel(isDBNull);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitCall(OpCodes.Callvirt, s_dataRecordIsDBNull, null);
            ilGenerator.Emit(OpCodes.Brtrue, incrementIndex);

            //// var columnName = dataReader.GetName(i);
            ilGenerator.MarkLabel(getColumnName);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitCall(OpCodes.Callvirt, s_dataRecordGetName, null);
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Brfalse, incrementIndex);

            for (int i = 0; i < objectInfo.TableInfo.Columns.Count; i++)
            {
                ColumnInfo column = objectInfo.TableInfo.Columns[i];
                columnLabels[i] = ilGenerator.DefineLabel();

                //// case "{PropertyName}"
                ilGenerator.Emit(OpCodes.Ldloc_2);
                ilGenerator.Emit(OpCodes.Ldstr, column.ColumnName);
                ilGenerator.Emit(OpCodes.Call, s_stringEquals);
                ilGenerator.Emit(OpCodes.Brtrue, columnLabels[i]);
            }

            ilGenerator.Emit(OpCodes.Br, incrementIndex);

            for (int i = 0; i < objectInfo.TableInfo.Columns.Count; i++)
            {
                ColumnInfo column = objectInfo.TableInfo.Columns[i];
                Type actualPropertyType = TypeConverter.ResolveActualType(column.PropertyInfo.PropertyType);

                //// case "{ColumnName}":
                ilGenerator.MarkLabel(columnLabels[i]);

                ilGenerator.Emit(OpCodes.Ldloc_0); //// {Type} instance

                if (TypeConverter.For(column.PropertyInfo.PropertyType) == null
                    && typeof(IDataRecord).GetMethod("Get" + actualPropertyType.Name) != null)
                {
                    //// var columnValue = dataReader.Get{PropertyType}(i);
                    ilGenerator.Emit(OpCodes.Ldarg_0); //// IDataReader
                    ilGenerator.Emit(OpCodes.Ldloc_1); //// int i
                    ilGenerator.EmitCall(OpCodes.Callvirt, typeof(IDataRecord).GetMethod("Get" + actualPropertyType.Name), null);
                }
                else
                {
                    if (TypeConverter.For(column.PropertyInfo.PropertyType) != null)
                    {
                        //// typeConverter = TypeConverter.For(propertyType);
                        ilGenerator.Emit(OpCodes.Ldtoken, column.PropertyInfo.PropertyType);
                        ilGenerator.EmitCall(OpCodes.Call, s_typeGetTypeFromHandleMethod, null);
                        ilGenerator.EmitCall(OpCodes.Call, s_typeConverterForMethod, null);
                    }
                    else
                    {
                        //// typeConverter = TypeConverter.Default;
                        ilGenerator.EmitCall(OpCodes.Call, s_typeConverterDefaultMethod, null);
                    }

                    ilGenerator.Emit(OpCodes.Ldarg_0); //// IDataReader
                    ilGenerator.Emit(OpCodes.Ldloc_1); //// int i

                    //// var columnValue = typeConverter.ConvertFromDbValue(dataReader, i, {propertyType});
                    ilGenerator.Emit(OpCodes.Ldtoken, column.PropertyInfo.PropertyType);
                    ilGenerator.EmitCall(OpCodes.Call, s_typeGetTypeFromHandleMethod, null);
                    ilGenerator.EmitCall(OpCodes.Callvirt, s_convertFromDbValueMethod, null);

                    //// columnValue = ({PropertyType})columnValue;
                    ilGenerator.EmitUnboxOrCast(actualPropertyType);
                }

                if (column.PropertyInfo.PropertyType.IsGenericType && typeof(Nullable<>).IsAssignableFrom(column.PropertyInfo.PropertyType.GetGenericTypeDefinition()))
                {
                    //// columnValue = new Nullable<{PropertyType}>(columnValue);
                    ilGenerator.Emit(OpCodes.Newobj, column.PropertyInfo.PropertyType.GetConstructor(new[] { actualPropertyType }));
                }

                //// entity.{Property} = columnValue;
                ilGenerator.EmitCall(OpCodes.Callvirt, column.PropertyInfo.GetSetMethod(), null);

                ilGenerator.Emit(OpCodes.Br, incrementIndex);
            }

            //// i++;
            ilGenerator.MarkLabel(incrementIndex);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitEfficientInt(1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc_1);

            //// if (i < dataReader.FieldCount)
            ilGenerator.MarkLabel(getFieldCount);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.EmitCall(OpCodes.Callvirt, s_dataRecordGetFieldCount, null);
            ilGenerator.Emit(OpCodes.Blt, isDBNull);

            //// return entity;
            ilGenerator.MarkLabel(returnEntity);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);

            var instanceFactory = (Func<IDataReader, object>)dynamicMethod.CreateDelegate(typeof(Func<IDataReader, object>));

            return instanceFactory;
        }

        internal static Action<object, object> CreateSetIdentifier(IObjectInfo objectInfo)
        {
            var dynamicMethod = new DynamicMethod(
                name: "MicroLite" + objectInfo.ForType.Name + "SetIdentifier",
                returnType: null,
                parameterTypes: new[] { typeof(object), typeof(object) }); //// arg_0, arg_1

            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            //// var instance = ({Type})arg_0;
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Castclass, objectInfo.ForType);

            //// var value = arg_1;
            ilGenerator.Emit(OpCodes.Ldarg_1);

            //// value = ({PropertyType})value;
            ilGenerator.EmitUnboxIfValueType(objectInfo.TableInfo.IdentifierColumn.PropertyInfo.PropertyType);

            //// instance.Id = value;
            ilGenerator.Emit(OpCodes.Callvirt, objectInfo.TableInfo.IdentifierColumn.PropertyInfo.GetSetMethod());

            ilGenerator.Emit(OpCodes.Ret);

            var setIdentifierValue = (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));

            return setIdentifierValue;
        }

        private static void EmitGetPropertyValues(ILGenerator ilGenerator, IObjectInfo objectInfo, Func<ColumnInfo, bool> includeColumn)
        {
            int index = 0;

            for (int i = 0; i < objectInfo.TableInfo.Columns.Count; i++)
            {
                ColumnInfo column = objectInfo.TableInfo.Columns[i];

                if (!includeColumn(column))
                {
                    continue;
                }

                ilGenerator.Emit(OpCodes.Ldloc_1); //// loc_1 - SqlArgument[] sqlArguments
                ilGenerator.EmitEfficientInt(index++);
                ilGenerator.Emit(OpCodes.Ldelema, typeof(SqlArgument));

                bool hasTypeConverter = TypeConverter.For(column.PropertyInfo.PropertyType) != null;

                if (hasTypeConverter)
                {
                    //// typeConverter = TypeConverter.For(propertyType);
                    ilGenerator.Emit(OpCodes.Ldtoken, column.PropertyInfo.PropertyType);
                    ilGenerator.EmitCall(OpCodes.Call, s_typeGetTypeFromHandleMethod, null);
                    ilGenerator.EmitCall(OpCodes.Call, s_typeConverterForMethod, null);
                }

                //// var value = entity.{PropertyName};
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.EmitCall(OpCodes.Callvirt, column.PropertyInfo.GetGetMethod(), null);

                //// value = (object)value;
                ilGenerator.EmitBoxIfValueType(column.PropertyInfo.PropertyType);

                if (hasTypeConverter)
                {
                    //// var converted = typeConverter.ConvertToDbValue(value, propertyType);
                    ilGenerator.Emit(OpCodes.Ldtoken, column.PropertyInfo.PropertyType);
                    ilGenerator.EmitCall(OpCodes.Call, s_typeGetTypeFromHandleMethod, null);
                    ilGenerator.EmitCall(OpCodes.Call, s_convertToDbValueMethod, null);
                }

                ilGenerator.EmitEfficientInt((int)column.DbType);
                ilGenerator.Emit(OpCodes.Newobj, s_sqlArgumentConstructor);

                //// sqlArguments[i] = new SqlArgument(value, column.DbType); OR sqlArguments[i] = new SqlArgument(converted, column.DbType);
                ilGenerator.Emit(OpCodes.Stobj, typeof(SqlArgument));
            }
        }
    }
}
