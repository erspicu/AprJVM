using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

//for typedefined (jvm spec style)
using u1 = System.Byte;
using u2 = System.UInt16;
using u4 = System.UInt32;
using u8 = System.UInt64;
using i1 = System.SByte;
using i2 = System.Int16;
using i4 = System.Int32;
using i8 = System.Int64 ;

namespace AprilJVM
{
    public class AprJVM
    {
        public Class_info classfile;
        long ptr_cur = 0;
        byte[] classbytes;

        public bool parse_classfile_ok = false;

        Dictionary<string, object> fields = new Dictionary<string, object>();

        public bool Execute()
        {

            if (!parse_classfile_ok)
                return false;

            try
            {
                //find clinit 預構子進入點
                bool clinit_entry_exist = false;
                Frame clinit_frame = null;
                foreach (method_info m in classfile.methods)
                {

                    if (classfile.constant_pool[m.name_index - 1].GetType().ToString().EndsWith("+CONSTANT_Utf8_info") && ((CONSTANT_Utf8_info)classfile.constant_pool[m.name_index - 1]).bytes_str == "<clinit>")
                    {
                        clinit_frame = new Frame(m);
                        clinit_entry_exist = true;
                        break;
                    }
                }

                if (clinit_entry_exist)
                {
                    ReturnParam rp_clinit = new ReturnParam();
                    if (!ExecuteFrame(ref clinit_frame, ref rp_clinit))
                        return false;
                }

                //find main
                bool main_entry_exist = false;
                Frame first_frame = null;
                foreach (method_info m in classfile.methods)
                {

                    if (classfile.constant_pool[m.name_index - 1].GetType().ToString().EndsWith("+CONSTANT_Utf8_info") && ((CONSTANT_Utf8_info)classfile.constant_pool[m.name_index - 1]).bytes_str == "main")
                    {
                        first_frame = new Frame(m);
                        main_entry_exist = true;
                        break;
                    }
                }

                if (!main_entry_exist)
                {
                    MessageBox.Show("無 main 進入點 !");
                    return false;
                }


                ReturnParam rp = new ReturnParam();
                if (!ExecuteFrame(ref first_frame, ref rp))
                    return false;


                MessageBox.Show("finish");

            }
            catch (Exception e)
            {
                Console.WriteLine("Execute() : " + e.Message);
                return false;
            }

            return true;
        }

        bool ExecuteFrame(ref Frame frame, ref ReturnParam RP)
        {
            byte code_save = 0;

            try
            {
                byte[] code = ((Code_attribute)frame.method.attributes[0].info).code;


                while (true)
                {

                    int pc_c = frame.PC;
                    byte opcode = 0;



                    try
                    {
                        opcode = code[frame.PC++];
                        code_save = opcode;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("opcode 抓取錯誤!");
                        return false;
                    }

                    #region for debug

                    #endregion

                    switch (opcode)
                    {
                        case 0x0://nop
                            break;

                        case 0x01://aconst_null
                            frame.opStackPush(null);
                            break;

                        case 0x02://iconst_m1
                            frame.opStackPush(-1);
                            break;

                        case 0x03://iconst_0                            
                            frame.opStackPush(0);
                            break;

                        case 0x04://iconst_1                            
                            frame.opStackPush(1);
                            break;

                        case 0x05://iconst_2                            
                            frame.opStackPush(2);
                            break;

                        case 0x06://iconst_3                           
                            frame.opStackPush(3);
                            break;

                        case 0x07://iconst_4
                            frame.opStackPush(4);
                            break;

                        case 0x08://iconst_5
                            frame.opStackPush(5);
                            break;

                        case 0x09://lconst_0
                            frame.opStackPush(0L);
                            break;

                        case 0x0a://lconst_1
                            frame.opStackPush(1L);
                            break;

                        case 0x0b://fconst_0
                            frame.opStackPush(0f);
                            break;

                        case 0x0c://fconst_1
                            frame.opStackPush(1f);
                            break;

                        case 0x0d://fconst_2
                            frame.opStackPush(2f);
                            break;

                        case 0x0e://dconst_0
                            frame.opStackPush(0.0);
                            break;

                        case 0x0f://dconst_1
                            frame.opStackPush(1.0);
                            break;



                        case 0x10://bipush                            
                            frame.opStackPush(((sbyte)code[frame.PC++]));
                            break;

                        case 0x11://sipush
                            frame.opStackPush((i4)((i2)(code[frame.PC++] << 8 | code[frame.PC++])));
                            break;

                        case 0x12://ldc
                            {

                                int index = code[frame.PC++] - 1;
                                string type = classfile.constant_pool[index].GetType().Name;
                                switch (type)
                                {
                                    case "CONSTANT_String_info":
                                        {
                                            string str = ((CONSTANT_Utf8_info)classfile.constant_pool[((CONSTANT_String_info)classfile.constant_pool[index]).string_index - 1]).bytes_str;
                                            frame.opStackPush(str);
                                        }
                                        break;
                                    case "CONSTANT_Float_info":
                                        {
                                            float f = ((CONSTANT_Float_info)classfile.constant_pool[index]).bytes;
                                            frame.opStackPush(f);
                                        }
                                        break;
                                    default:
                                        MessageBox.Show("ldc - 未處理到的轉換型別!");
                                        break;
                                }
                            }
                            break;

                        case 0x13://ldc_w
                            {

                                int index = (code[frame.PC++] << 8 | code[frame.PC++]) - 1;
                                string type = classfile.constant_pool[index].GetType().Name;
                                switch (type)
                                {
                                    case "CONSTANT_String_info":
                                        {
                                            string str = ((CONSTANT_Utf8_info)classfile.constant_pool[((CONSTANT_String_info)classfile.constant_pool[index]).string_index - 1]).bytes_str;
                                            frame.opStackPush(str);
                                        }
                                        break;
                                    case "CONSTANT_Float_info":
                                        {
                                            float f = ((CONSTANT_Float_info)classfile.constant_pool[index]).bytes;
                                            frame.opStackPush(f);
                                        }
                                        break;
                                    default:
                                        MessageBox.Show("ldc - 未處理到的轉換型別!");
                                        break;
                                }
                            }
                            break;

                        case 0x14://ldc2_w
                            {
                                int index = (code[frame.PC++] << 8 | code[frame.PC++]) - 1;
                                string type = classfile.constant_pool[index].GetType().Name;
                                switch (type)
                                {
                                    case "CONSTANT_Long_info":
                                        {
                                            CONSTANT_Long_info long_inf = (CONSTANT_Long_info)classfile.constant_pool[index];
                                            long t = (long)((ulong)long_inf.high_bytes << 32 | (ulong)long_inf.low_bytes);
                                            frame.opStackPush(t);
                                        }
                                        break;

                                    case "CONSTANT_Double_info":
                                        {
                                            CONSTANT_Double_info Double_inf = (CONSTANT_Double_info)classfile.constant_pool[index];
                                            frame.opStackPush(Double_inf.value);
                                        }
                                        break;


                                    default:
                                        MessageBox.Show("ldcc_w - 未處理到的轉換型別!");
                                        break;
                                }
                            }
                            break;

                        case 0x15://iload          
                            frame.opStackPush((i4)frame.LocalVariable[code[frame.PC++]]);
                            break;

                        case 0x16://lload
                            frame.opStackPush(frame.LocalVariable[code[frame.PC++]]);
                            break;

                        case 0x17://fload
                            frame.opStackPush(frame.LocalVariable[code[frame.PC++]]);
                            break;

                        case 0x18://dload
                            frame.opStackPush(frame.LocalVariable[code[frame.PC++]]);
                            break;

                        case 0x1a://iload_0
                            frame.opStackPush((i4)frame.LocalVariable[0]);
                            break;

                        case 0x1b://iload_1 
                            frame.opStackPush((i4)frame.LocalVariable[1]);
                            break;

                        case 0x1c://iload_2 
                            frame.opStackPush((i4)frame.LocalVariable[2]);
                            break;

                        case 0x1d://iload_3
                            frame.opStackPush((i4)frame.LocalVariable[3]);
                            break;

                        case 0x1e://lload_0                         
                            frame.opStackPush(frame.LocalVariable[0]);
                            break;

                        case 0x1f://lload_1                         
                            frame.opStackPush(frame.LocalVariable[1]);
                            break;

                        case 0x20://lload_2                         
                            frame.opStackPush(frame.LocalVariable[2]);
                            break;

                        case 0x21://lload_3
                            frame.opStackPush(frame.LocalVariable[3]);
                            break;

                        case 0x22://fload_0
                            frame.opStackPush(frame.LocalVariable[0]);
                            break;

                        case 0x23://fload_1
                            frame.opStackPush(frame.LocalVariable[1]);
                            break;

                        case 0x24://fload_2
                            frame.opStackPush(frame.LocalVariable[2]);
                            break;

                        case 0x25://fload_3
                            frame.opStackPush(frame.LocalVariable[3]);
                            break;


                        case 0x26://dload_0
                            frame.opStackPush(frame.LocalVariable[0]);
                            break;

                        case 0x27://dload_1
                            frame.opStackPush(frame.LocalVariable[1]);
                            break;

                        case 0x28://dload_0
                            frame.opStackPush(frame.LocalVariable[2]);
                            break;

                        case 0x29://dload_3
                            frame.opStackPush(frame.LocalVariable[3]);
                            break;

                        case 0x2b://aload_1
                            frame.opStackPush(frame.LocalVariable[1]);
                            break;

                        case 0x2e://iload n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x2f://lload n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x30://fload n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x31://dload n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;


                        case 0x36://istore n                            
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x37://lstore n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x38://fstore n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;

                        case 0x39://dstore n
                            frame.LocalVariable[code[frame.PC++]] = frame.opStackPop();
                            break;


                        case 0x3b://istore_0                            
                            frame.LocalVariable[0] = frame.opStackPop();
                            break;

                        case 0x3c://istore_1                            
                            frame.LocalVariable[1] = frame.opStackPop();
                            break;

                        case 0x3d://istore_2                            
                            frame.LocalVariable[2] = frame.opStackPop();
                            break;

                        case 0x3e://istore_3                            
                            frame.LocalVariable[3] = frame.opStackPop();
                            break;

                        case 0x3f://lstore_0
                            frame.LocalVariable[0] = frame.opStackPop();
                            break;

                        case 0x40://lstore_1
                            frame.LocalVariable[1] = frame.opStackPop();
                            break;

                        case 0x41://lstore_2
                            frame.LocalVariable[2] = frame.opStackPop();
                            break;

                        case 0x42://lstore_3
                            frame.LocalVariable[3] = frame.opStackPop();
                            break;

                        case 0x43://fstore_0
                            frame.LocalVariable[0] = frame.opStackPop();
                            break;

                        case 0x44://fstore_1
                            frame.LocalVariable[1] = frame.opStackPop();
                            break;

                        case 0x45://fstore_2
                            frame.LocalVariable[2] = frame.opStackPop();
                            break;

                        case 0x46://fstore_3
                            frame.LocalVariable[3] = frame.opStackPop();
                            break;

                        case 0x4c://astore_1 
                            frame.LocalVariable[1] = frame.opStackPop();
                            break;

                        case 0x47://dstore_0
                            frame.LocalVariable[0] = frame.opStackPop();
                            break;

                        case 0x48://dstore_1
                            frame.LocalVariable[1] = frame.opStackPop();
                            break;

                        case 0x49://dstore_2
                            frame.LocalVariable[2] = frame.opStackPop();
                            break;

                        case 0x4a://dstore_3
                            frame.LocalVariable[3] = frame.opStackPop();
                            break;

                        case 0x57://pop
                            frame.opStackPop();
                            break;

                        case 0x58://pop2
                            {
                                object o = frame.opStackPop();
                                if (o.GetType().Name != "Double" && o.GetType().Name != "Int64")
                                    frame.opStackPop();
                            }
                            break;

                        case 0x59://dup
                            frame.opStackPush(frame.OperandStack.Last());
                            break;

                        case 0x5f://swap
                            {
                                object o1 = frame.opStackPop();
                                object o2 = frame.opStackPop();
                                frame.opStackPush(o2);
                                frame.opStackPush(o1);
                            }
                            break;

                        case 0x60://iadd
                            frame.opStackPush((i4)frame.opStackPop() + (i4)frame.opStackPop());
                            break;

                        case 0x61://ladd
                            frame.opStackPush((long)frame.opStackPop() + (long)frame.opStackPop());
                            break;

                        case 0x62://fadd
                            frame.opStackPush((float)frame.opStackPop() + (float)frame.opStackPop());
                            break;

                        case 0x63://dadd
                            frame.opStackPush((double)frame.opStackPop() + (double)frame.opStackPop());
                            break;

                        case 0x64://isub
                            frame.opStackPush(-(i4)frame.opStackPop() + (i4)frame.opStackPop());
                            break;

                        case 0x65://lsub
                            frame.opStackPush(-(long)frame.opStackPop() + (long)frame.opStackPop());
                            break;

                        case 0x66://fsub
                            frame.opStackPush(-(float)frame.opStackPop() + (float)frame.opStackPop());
                            break;

                        case 0x67://dsub
                            frame.opStackPush(-(double)frame.opStackPop() + (double)frame.opStackPop());
                            break;

                        case 0x68://imul
                            frame.opStackPush((i4)frame.opStackPop() * (i4)frame.opStackPop());
                            break;

                        case 0x69://lmul
                            frame.opStackPush((long)frame.opStackPop() * (long)frame.opStackPop());
                            break;

                        case 0x6a://fmul
                            frame.opStackPush((float)frame.opStackPop() * (float)frame.opStackPop());
                            break;

                        case 0x6b://dmul
                            frame.opStackPush((double)frame.opStackPop() * (double)frame.opStackPop());
                            break;

                        case 0x6c://idiv
                            {
                                int t1 = (i4)frame.opStackPop();
                                int t2 = (i4)frame.opStackPop();
                                frame.opStackPush(t2 / t1);
                            }
                            break;

                        case 0x6d://ldiv
                            {
                                long t1 = (long)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();
                                frame.opStackPush(t2 / t1);
                            }
                            break;

                        case 0x6e://fdiv
                            {
                                float t1 = (float)frame.opStackPop();
                                float t2 = (float)frame.opStackPop();
                                frame.opStackPush(t2 / t1);
                            }
                            break;

                        case 0x6f://ddiv
                            {
                                double t1 = (double)frame.opStackPop();
                                double t2 = (double)frame.opStackPop();
                                frame.opStackPush(t2 / t1);
                            }
                            break;

                        case 0x70://irem
                            {
                                int t1 = (i4)frame.opStackPop();
                                int t2 = (i4)frame.opStackPop();
                                frame.opStackPush(t2 % t1);
                            }
                            break;

                        case 0x71://lrem
                            {
                                long t1 = (long)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();
                                frame.opStackPush(t2 % t1);
                            }
                            break;
                        case 0x72://frem
                            {
                                float t1 = (float)frame.opStackPop();
                                float t2 = (float)frame.opStackPop();
                                frame.opStackPush(t2 % t1);
                            }
                            break;
                        case 0x73://drem
                            {
                                double t1 = (double)frame.opStackPop();
                                double t2 = (double)frame.opStackPop();
                                frame.opStackPush(t2 % t1);
                            }
                            break;


                        case 0x74://ineg
                            frame.opStackPush(-(i4)frame.opStackPop());
                            break;

                        case 0x75://lneg
                            frame.opStackPush(-(long)frame.opStackPop());
                            break;

                        case 0x76://fneg
                            frame.opStackPush(-(float)frame.opStackPop());
                            break;

                        case 0x77://dneg
                            frame.opStackPush(-(double)frame.opStackPop());
                            break;

                        case 0x78://ishl
                            {
                                int t1 = (i4)frame.opStackPop();
                                int t2 = (i4)frame.opStackPop();
                                frame.opStackPush(t2 << t1);
                            }
                            break;

                        case 0x79://lshl ??
                            {
                                i4 t1 = (i4)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();
                                frame.opStackPush(t2 << t1);
                            }
                            break;

                        case 0x7a://ishr
                            {
                                int t1 = (i4)frame.opStackPop();
                                int t2 = (i4)frame.opStackPop();
                                frame.opStackPush(t2 >> t1);
                            }
                            break;

                        case 0x7b://lshr
                            {
                                int t1 = (i4)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();
                                frame.opStackPush(t2 >> t1);
                            }
                            break;

                        case 0x7c://iushr
                            {
                                int t1 = (i4)frame.opStackPop();
                                int t2 = (i4)frame.opStackPop();
                                frame.opStackPush( (i4)((u4)t2 >> t1));
                            }
                            break;

                        case 0x7d://lushr
                            {
                                int t1 = (i4)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();
                                frame.opStackPush((long)((ulong)t2 >> t1));
                            }
                            break;

                        case 0x7e://iand
                            frame.opStackPush((i4)frame.opStackPop() & (i4)frame.opStackPop());
                            break;

                        case 0x7f://land
                            frame.opStackPush((long)frame.opStackPop() & (long)frame.opStackPop());
                            break;


                        case 0x80://ior
                            frame.opStackPush((i4)frame.opStackPop() | (i4)frame.opStackPop());
                            break;

                        case 0x81://lor
                            frame.opStackPush((long)frame.opStackPop() | (long)frame.opStackPop());
                            break;

                        case 0x82://ixor
                            frame.opStackPush((i4)frame.opStackPop() ^ (i4)frame.opStackPop());
                            break;

                        case 0x83://lxor
                            frame.opStackPush((long)frame.opStackPop() ^ (long)frame.opStackPop());
                            break;

                        case 0x84://innc
                            {
                                int index = code[frame.PC++];
                                int val = (sbyte)code[frame.PC++];
                                frame.LocalVariable[index] = (i4)frame.LocalVariable[index] + val;
                            }
                            break;

                        case 0x85://i2l
                            frame.opStackPush((long)(i4)frame.opStackPop());
                            break;

                        case 0x86://i2f
                            frame.opStackPush((float)(i4)frame.opStackPop());
                            break;

                        case 0x87://i2d
                            frame.opStackPush((double)(i4)frame.opStackPop());
                            break;

                        case 0x88://l2i
                            frame.opStackPush((int)(long)frame.opStackPop());
                            break;

                        case 0x89://l2f
                            frame.opStackPush((float)(long)frame.opStackPop());
                            break;

                        case 0x8a://l2d
                            frame.opStackPush((double)(long)frame.opStackPop());
                            break;


                        case 0x8b://f2i
                            frame.opStackPush((i4)(float)frame.opStackPop());
                            break;

                        case 0x8c://f2l
                            frame.opStackPush((long)(float)frame.opStackPop());
                            break;

                        case 0x8d://f2d
                            frame.opStackPush((double)(float)frame.opStackPop());
                            break;

                        //

                        case 0x8e://d2i
                            frame.opStackPush((i4)(double)frame.opStackPop());
                            break;

                        case 0x8f://d2l
                            frame.opStackPush((long)(double)frame.opStackPop());
                            break;


                        case 0x90://d2f
                            frame.opStackPush((float)(double)frame.opStackPop());
                            break;

                        //

                        case 0x91://i2b
                            frame.opStackPush((i4)(i1)((i4)frame.opStackPop() & 0xff));
                            break;

                        case 0x92://i2c
                            frame.opStackPush((i4)(u1)((i4)frame.opStackPop() & 0xff));
                            break;


                        case 0x93://i2s
                            frame.opStackPush((i4)(i2)((i4)frame.opStackPop() & 0xff));
                            break;

                        case 0x94://lcmp
                            {
                                long t1 = (long)frame.opStackPop();
                                long t2 = (long)frame.opStackPop();

                                if (t1 == t2)
                                    frame.opStackPush((int)0);

                                if (t1 < t2)
                                    frame.opStackPush((int)1);

                                if (t1 > t2)
                                    frame.opStackPush((int)-1);
                            }
                            break;


                        case 0x95://fcmpl
                            {
                                float t1 = (float)frame.opStackPop();
                                float t2 = (float)frame.opStackPop();

                                if (t1 == t2)
                                    frame.opStackPush((int)0);

                                if (t1 < t2)
                                    frame.opStackPush((int)1);

                                if (t1 > t2)
                                    frame.opStackPush((int)-1);
                            }
                            break;

                        case 0x96://fcmpg
                            {
                                float t1 = (float)frame.opStackPop();
                                float t2 = (float)frame.opStackPop();

                                if (t1 == t2)
                                    frame.opStackPush((int)0);

                                if (t1 < t2)
                                    frame.opStackPush((int)1);

                                if (t1 > t2)
                                    frame.opStackPush((int)-1);
                            }
                            break;

                        case 0x97://dcmpl
                            {
                                double t1 = (double)frame.opStackPop();
                                double t2 = (double)frame.opStackPop();

                                if (t1 == t2)
                                    frame.opStackPush((int)0);

                                if (t1 < t2)
                                    frame.opStackPush((int)1);

                                if (t1 > t2)
                                    frame.opStackPush((int)-1);
                            }
                            break;

                        case 0x98://dcmpg
                            {
                                double t1 = (double)frame.opStackPop();
                                double t2 = (double)frame.opStackPop();

                                if (t1 == t2)
                                    frame.opStackPush((int)0);

                                if (t1 < t2)
                                    frame.opStackPush((int)1);

                                if (t1 > t2)
                                    frame.opStackPush((int)-1);
                            }
                            break;


                        case 0x99://ifeq
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() == 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9a://ifne
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() != 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9b://iflt
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() < 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9c://ifge
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() >= 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9d://ifgt
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() > 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9e://ifle
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() <= 0)
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0x9f://if_icmpeq
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() == (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa0://if_icmpne
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() != (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa1://if_icmplt
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() > (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa2://if_icmpge
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() <= (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa3://if_icmplg
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() < (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa4://if_icmple
                            {
                                int PC_OFFSET = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if ((i4)frame.opStackPop() >= (i4)frame.opStackPop())
                                    frame.PC += PC_OFFSET - 3; // 需要扣除掉撈取opcode時,預先做的PC累計以及撈取pc offset佔用的PC累計,因此 -3
                            }
                            break;

                        case 0xa7://goto                                                       
                            frame.PC += ((short)((code[frame.PC++] << 8) | code[frame.PC++])) - 1;
                            break;

                        case 0xaa://tableswitch
                            {
                                int pc_now = frame.PC - 1;
                                frame.PC += 4 - (frame.PC % 4); // for word padding
                                int default_offset = (code[frame.PC++] << 24 | code[frame.PC++] << 16 | code[frame.PC++] << 8 | code[frame.PC++]);
                                int min = (code[frame.PC++] << 24 | code[frame.PC++] << 16 | code[frame.PC++] << 8 | code[frame.PC++]);
                                int max = (code[frame.PC++] << 24 | code[frame.PC++] << 16 | code[frame.PC++] << 8 | code[frame.PC++]);
                                int v = (i4)frame.opStackPop();
                                if (v > max || v < min)
                                    frame.PC = default_offset + pc_now;
                                else
                                {
                                    int index = v - min;
                                    frame.PC += index * 4;
                                    int offset = (code[frame.PC++] << 24 | code[frame.PC++] << 16 | code[frame.PC++] << 8 | code[frame.PC++]);
                                    frame.PC = offset + pc_now;
                                }
                            }
                            break;

                        case 0xac://ireturn
                            {
                                RP.void_return = false;
                                RP.return_obj = (i4)frame.opStackPop();
                                return true;
                            }

                        case 0xad://lreturn
                            {
                                RP.void_return = false;
                                RP.return_obj = (long)frame.opStackPop();
                                return true;
                            }

                        case 0xae://freturn
                            {
                                RP.void_return = false;
                                RP.return_obj = (float)frame.opStackPop();
                                return true;
                            }

                        case 0xaf://dreturn
                            {
                                RP.void_return = false;
                                RP.return_obj = (double)frame.opStackPop();
                                return true;
                            }


                        case 0xb1://return
                            {
                                RP.void_return = true;
                                return true;
                            }

                        case 0xb2://getstatic
                            {
                                int field_index = (code[frame.PC++] << 8) | code[frame.PC++];
                                int name_and_type_index = ((CONSTANT_Fieldref_info)classfile.constant_pool[field_index - 1]).name_and_type_index;
                                int name_index = ((CONSTANT_NameAndType_info)classfile.constant_pool[name_and_type_index - 1]).name_index;
                                string key = ((CONSTANT_Utf8_info)classfile.constant_pool[name_index - 1]).bytes_str;
                                if (fields.ContainsKey(key)) // ?
                                    frame.opStackPush(fields[key]);
                            }
                            break;

                        case 0xb3://putstatic
                            {
                                int field_index = (code[frame.PC++] << 8) | code[frame.PC++];
                                int name_and_type_index = ((CONSTANT_Fieldref_info)classfile.constant_pool[field_index - 1]).name_and_type_index;
                                int name_index = ((CONSTANT_NameAndType_info)classfile.constant_pool[name_and_type_index - 1]).name_index;
                                string key = ((CONSTANT_Utf8_info)classfile.constant_pool[name_index - 1]).bytes_str;
                                fields[key] = frame.opStackPop();
                            }
                            break;

                        case 0xb6://invokevirtual
                            {
                                int invoke_index = (code[frame.PC++] << 8) | code[frame.PC++];
                                object item = frame.opStackPop();

                                switch (item.GetType().Name)
                                {
                                    case "String":
                                        Console.WriteLine((string)item);
                                        break;

                                    case "Int32":
                                        Console.WriteLine((i4)item);
                                        break;

                                    case "Int64":
                                        Console.WriteLine((long)item);
                                        break;

                                    case "Single":
                                        Console.WriteLine((float)item);
                                        break;

                                    case "Double":
                                        Console.WriteLine((double)item);
                                        break;

                                    default:
                                        MessageBox.Show("未處理到的型別 " + item.GetType().Name);
                                        break;
                                }
                            }
                            break;

                        case 0xb8://invokestatic
                            {
                                int invoke_index = (code[frame.PC++] << 8) | code[frame.PC++];
                                string method_name = ((CONSTANT_Utf8_info)classfile.constant_pool[((CONSTANT_NameAndType_info)classfile.constant_pool[((CONSTANT_Methodref_info)classfile.constant_pool[invoke_index - 1]).name_and_type_index - 1]).name_index - 1]).bytes_str;
                                Frame invoke_frame = null;
                                foreach (method_info m in classfile.methods)
                                {
                                    if (classfile.constant_pool[m.name_index - 1].GetType().ToString().EndsWith("+CONSTANT_Utf8_info") && ((CONSTANT_Utf8_info)classfile.constant_pool[m.name_index - 1]).bytes_str == method_name)
                                    {
                                        invoke_frame = new Frame(m);
                                        break;
                                    }
                                }

                                string method_format = ((CONSTANT_Utf8_info)classfile.constant_pool[invoke_frame.method.descriptor_index - 1]).bytes_str;
                                method_format = method_format.Remove(0, 1);
                                int params_count = method_format.Split(new char[] { ')' })[0].Length;


                                for (int i = 1; i <= params_count; i++)
                                    invoke_frame.LocalVariable[params_count - i] = frame.opStackPop();

                                ReturnParam rp = new ReturnParam();

                                if (!ExecuteFrame(ref invoke_frame, ref rp)) return false;
                                if (rp.void_return != true) frame.opStackPush(rp.return_obj);
                            }
                            break;

                        case 0xc6://ifnull
                            {
                                int offset = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if (frame.opStackPop() == null)
                                    frame.PC += offset - 3;
                            }
                            break;

                        case 0xc7://ifnonull
                            {
                                int offset = (short)((code[frame.PC++] << 8) | code[frame.PC++]);
                                if (frame.opStackPop() != null)
                                    frame.PC += offset - 3;
                            }
                            break;

                        case 0xc8://goto_w
                            {
                                int offset = (int)((code[frame.PC++] << 24) | (code[frame.PC++] << 16) | (code[frame.PC++] << 8) | code[frame.PC++]);
                                frame.PC += offset - 5;
                            }
                            break;

                        default: MessageBox.Show("處理錯誤,或是有尚未實做的opcode " + opcode.ToString("x2"));
                            return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ExecuteFrame() : " + e.Message + " opcode:" + code_save.ToString("X2"));
            }
            return true;
        }

        public class ReturnParam
        {

            public bool void_return = true;
            public object return_obj = null;

        }

        //ref https://docs.oracle.com/javase/specs/jvms/se7/html/jvms-4.html
        public bool LoadClass(string filepath)
        {
            try
            {
                classbytes = File.ReadAllBytes(filepath);
            }
            catch (Exception e)
            {
                Console.WriteLine(" LoadClass() : " + e.Message);
                parse_classfile_ok = false;
                return false;
            }
            bool result = parse_class_struct();
            parse_classfile_ok = result;
            return result;
        }


        #region struct parse
        public bool parse_class_struct(byte[] filebytes)
        {
            classbytes = filebytes;
            return parse_class_struct();
        }

        bool parse_class_struct()
        {
            try
            {
                ptr_cur = 0;
                classfile = new Class_info();
                classfile.magic = (uint)(classbytes[ptr_cur++] << 24 | classbytes[ptr_cur++] << 16 | classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                if (classfile.magic != 0xCAFEBABE) return false;
                classfile.minor_version = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.major_version = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.constant_pool_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.constant_pool = new object[classfile.constant_pool_count - 1];
                if (!parse_constant_pool()) return false;
                classfile.access_flags = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.this_class = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.super_class = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.interfaces_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.interfaces = new u2[classfile.interfaces_count];
                for (int i = 0; i < classfile.interfaces_count; i++)
                    classfile.interfaces[i] = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.fields_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.fields = new field_info[classfile.fields_count];
                if (!parse_fields()) return false;
                classfile.methods_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.methods = new method_info[classfile.methods_count];
                if (!parse_methods()) return false;
                classfile.attributes_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                classfile.attributes = new attribute_info[classfile.attributes_count];
                if (!parse_attributes(ref classfile.attributes, classfile.attributes_count)) return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("parse_class_struct() : " + e.Message);
                return false;
            }
            return true;
        }

        bool parse_fields()
        {
            try
            {
                for (int i = 0; i < classfile.fields_count; i++)
                {
                    field_info t = new field_info();
                    t.access_flags = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.descriptor_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.attributes_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.attributes = new attribute_info[t.attributes_count];
                    if (!parse_attributes(ref t.attributes, t.attributes_count)) return false;
                    classfile.fields[i] = t;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("parse_fields() : " + e.Message);
                return false;
            }
            return true;
        }

        bool parse_methods()
        {
            try
            {
                for (int i = 0; i < classfile.methods_count; i++)
                {
                    method_info t = new method_info();
                    t.access_flags = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.descriptor_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.attributes_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.attributes = new attribute_info[t.attributes_count];
                    if (!parse_attributes(ref t.attributes, t.attributes_count)) return false;
                    classfile.methods[i] = t;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("parse_methods() : " + e.Message);
            }
            return true;
        }

        bool parse_attributes(ref attribute_info[] attributes, u2 counts)
        {
            try
            {
                for (int i = 0; i < counts; i++)
                {
                    attribute_info t = new attribute_info();

                    t.attribute_name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                    t.attribute_length = (u4)((classbytes[ptr_cur++] << 24) | (classbytes[ptr_cur++] << 16) | (classbytes[ptr_cur++] << 8) | (classbytes[ptr_cur++]));

                    string attr_str = ((CONSTANT_Utf8_info)classfile.constant_pool[t.attribute_name_index - 1]).bytes_str;
                    switch (attr_str)
                    {
                        case "Code":
                            {
                                Code_attribute temp = new Code_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.max_stack = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.max_locals = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.code_length = (u4)((classbytes[ptr_cur++] << 24) | (classbytes[ptr_cur++] << 16) | (classbytes[ptr_cur++] << 8) | (classbytes[ptr_cur++]));
                                temp.code = new u1[temp.code_length];
                                for (int x = 0; x < temp.code_length; x++) temp.code[x] = classbytes[ptr_cur++];
                                temp.exception_table_length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.exception_table = new exception[temp.exception_table_length];
                                for (int x = 0; x < temp.exception_table_length; x++)
                                {
                                    exception temp_exception = new exception();
                                    temp_exception.start_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_exception.end_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_exception.handler_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_exception.catch_type = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.exception_table[x] = temp_exception;

                                }
                                temp.attributes_count = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.attributes = new attribute_info[temp.attributes_count];
                                if (!parse_attributes(ref temp.attributes, temp.attributes_count)) return false;
                                t.info = temp;
                            }
                            break;

                        case "LineNumberTable":
                            {
                                LineNumberTable_attribute temp = new LineNumberTable_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.line_number_table_length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.line_number_table = new line_number[temp.line_number_table_length];
                                for (int x = 0; x < temp.line_number_table_length; x++)
                                {
                                    line_number temp_line_number = new line_number();
                                    temp_line_number.start_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_line_number.line_numbers = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.line_number_table[x] = temp_line_number;
                                }
                                t.info = temp;
                            }
                            break;

                        case "ConstantValue":
                            {
                                ConstantValue_attribute temp = new ConstantValue_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.constantvalue_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.info = temp;
                            }
                            break;

                        case "SourceFile":
                            {
                                SourceFile_attribute temp = new SourceFile_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.sourcefile_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.info = temp;
                            }
                            break;

                        case "InnerClasses":
                            {
                                InnerClasses_attribute temp = new InnerClasses_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.number_of_classes = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.classes = new number_of_classes[temp.number_of_classes];

                                for (int j = 0; j < temp.number_of_classes; j++)
                                {
                                    number_of_classes c = new number_of_classes();
                                    c.inner_class_info_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.outer_class_info_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.inner_name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.inner_class_access_flags = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]); ;
                                    temp.classes[j] = c;
                                }
                                t.info = temp;
                            }
                            break;

                        case "LocalVariableTable":
                            {
                                LocalVariableTable_attribute temp = new LocalVariableTable_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.local_variable_table_length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.local_variable_table = new local_variable[temp.local_variable_table_length];

                                for (int j = 0; j < temp.local_variable_table_length; j++)
                                {
                                    local_variable c = new local_variable();
                                    c.start_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.descriptor_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    c.index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.local_variable_table[j] = c;

                                }
                                t.info = temp;
                            }
                            break;


                        case "Exceptions":
                            {
                                Exceptions_attribute temp = new Exceptions_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.number_of_exceptions = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.exception_index_table = new u2[temp.number_of_exceptions];
                                for (int x = 0; x < temp.number_of_exceptions; x++)
                                {
                                    temp.exception_index_table[x] = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]); ;
                                }
                                t.info = temp;

                            }
                            break;

                        case "Signature":
                            {
                                Signature_attribute temp = new Signature_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.signature_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.info = temp;
                            }
                            break;

                        case "Synthetic":
                            {
                                Synthetic_attribute temp = new Synthetic_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;

                                t.info = temp;
                            }
                            break;

                        case "EnclosingMethod":
                            {
                                EnclosingMethod_attribute temp = new EnclosingMethod_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.class_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.method_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.info = temp;
                            }
                            break;

                        case "LocalVariableTypeTable":
                            {
                                LocalVariableTypeTable_attribute temp = new LocalVariableTypeTable_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.local_variable_type_table_length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.local_variable_type_table = new local_variable_type[temp.local_variable_type_table_length];
                                for (int x = 0; x < temp.local_variable_type_table_length; x++)
                                {
                                    local_variable_type temp_local_variable_type = new local_variable_type();

                                    temp_local_variable_type.start_pc = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]); ;
                                    temp_local_variable_type.length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_local_variable_type.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_local_variable_type.signature_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_local_variable_type.index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.local_variable_type_table[x] = temp_local_variable_type;
                                }
                                t.info = temp;
                            }
                            break;

                        case "Deprecated":
                            {
                                Deprecated_attribute temp = new Deprecated_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                t.info = temp;
                            }
                            break;

                        case "SourceDebugExtension":
                            {
                                SourceDebugExtension_attribute temp = new SourceDebugExtension_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.debug_extension = new u1[temp.attribute_length];
                                for (int x = 0; x < temp.attribute_length; x++)
                                    temp.debug_extension[x] = (u1)classbytes[ptr_cur++];
                                t.info = temp;
                            }
                            break;

                        case "BootstrapMethods":
                            {
                                BootstrapMethods_attribute temp = new BootstrapMethods_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.num_bootstrap_methods = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                temp.bootstrap_methods = new bootstrap_method[temp.num_bootstrap_methods];

                                for (int x = 0; x < temp.num_bootstrap_methods; x++)
                                {
                                    bootstrap_method bootstrap_method_temp = new bootstrap_method();
                                    bootstrap_method_temp.bootstrap_method_ref = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    bootstrap_method_temp.num_bootstrap_arguments = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    bootstrap_method_temp.bootstrap_arguments = new u2[bootstrap_method_temp.num_bootstrap_arguments];
                                    for (int y = 0; y < bootstrap_method_temp.num_bootstrap_arguments; y++)
                                        bootstrap_method_temp.bootstrap_arguments[y] = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.bootstrap_methods[x] = bootstrap_method_temp;
                                }
                                t.info = temp;
                            }
                            break;

                        case "MethodParameters":
                            {
                                MethodParameters_attribute temp = new MethodParameters_attribute();
                                temp.attribute_name_index = t.attribute_name_index;
                                temp.attribute_length = t.attribute_length;
                                temp.parameters_count = (u1)classbytes[ptr_cur++];
                                temp.parameters = new parameter[temp.parameters_count];
                                for (int x = 0; x < temp.parameters_count; x++)
                                {
                                    parameter temp_parameter = new parameter();
                                    temp_parameter.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp_parameter.access_flags = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                    temp.parameters[x] = temp_parameter;
                                }
                                t.info = temp;
                            }
                            break;

                        case "StackMapTable":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "StackMapTable parse editing";
                            }
                            break;

                        case "RuntimeVisibleAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeVisibleAnnotations parse editing";
                            }
                            break;

                        case "RuntimeInvisibleAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeInvisibleAnnotations parse editing";
                            }
                            break;

                        case "RuntimeVisibleParameterAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeVisibleParameterAnnotations parse editing";
                            }
                            break;

                        case "RuntimeInvisibleParameterAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeInvisibleParameterAnnotations parse editing";
                            }
                            break;

                        case "RuntimeVisibleTypeAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeVisibleTypeAnnotations parse editing";
                            }
                            break;

                        case "RuntimeInvisibleTypeAnnotations":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "RuntimeInvisibleTypeAnnotations parse editing";
                            }
                            break;

                        case "AnnotationDefault":
                            {
                                for (int x = 0; x < t.attribute_length; x++) ptr_cur++;
                                t.info = "AnnotationDefault parse editing";
                            }
                            break;

                        default:
                            MessageBox.Show("attribute " + attr_str + " parse editing..");
                            return false;

                    }


                    attributes[i] = t;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("parse_attributes() : " + e.Message);
                return false;
            }
            return true;
        }

        bool parse_constant_pool()
        {
            try
            {
                for (int i = 0; i < classfile.constant_pool_count - 1; i++)
                {
                    u1 tag = classbytes[ptr_cur++];
                    switch ((CONSTANT_type)tag)
                    {
                        case CONSTANT_type.CONSTANT_Class:
                            {
                                CONSTANT_Class_info t = new CONSTANT_Class_info();
                                t.tag = tag;
                                t.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;// c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Fieldref:
                            {
                                CONSTANT_Fieldref_info t = new CONSTANT_Fieldref_info();
                                t.tag = tag;
                                t.class_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.name_and_type_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t; // c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Methodref:
                            {
                                CONSTANT_Methodref_info t = new CONSTANT_Methodref_info();
                                t.tag = tag;
                                t.class_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.name_and_type_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;// c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_InterfaceMethodref:
                            {
                                CONSTANT_InterfaceMethodref_info t = new CONSTANT_InterfaceMethodref_info();
                                t.tag = tag;
                                t.class_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.name_and_type_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;// c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_String:
                            {
                                CONSTANT_String_info t = new CONSTANT_String_info();
                                t.tag = tag;
                                t.string_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t; //c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Integer:
                            {
                                CONSTANT_Integer_info t = new CONSTANT_Integer_info();
                                t.tag = tag;
                                t.bytes = (i4)(classbytes[ptr_cur++] << 24 | classbytes[ptr_cur++] << 16 | classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;//c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Float:
                            {
                                CONSTANT_Float_info t = new CONSTANT_Float_info();
                                t.tag = tag;
                                byte v0 = classbytes[ptr_cur++];
                                byte v1 = classbytes[ptr_cur++];
                                byte v2 = classbytes[ptr_cur++];
                                byte v3 = classbytes[ptr_cur++];
                                t.bytes = BitConverter.ToSingle(new byte[] { v3, v2, v1, v0 }, 0);
                                classfile.constant_pool[i] = t;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Long:
                            {
                                CONSTANT_Long_info t = new CONSTANT_Long_info();
                                t.tag = tag;
                                t.high_bytes = (u4)(classbytes[ptr_cur++] << 24 | classbytes[ptr_cur++] << 16 | classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.low_bytes = (u4)(classbytes[ptr_cur++] << 24 | classbytes[ptr_cur++] << 16 | classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i++] = t;//c_inf;
                                large_numeric t2 = new large_numeric();
                                t2.inf = "Long large numeric part";
                                classfile.constant_pool[i] = t2;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Double:
                            {
                                CONSTANT_Double_info t = new CONSTANT_Double_info();
                                t.tag = tag;

                                byte v0 = classbytes[ptr_cur++];
                                byte v1 = classbytes[ptr_cur++];
                                byte v2 = classbytes[ptr_cur++];
                                byte v3 = classbytes[ptr_cur++];
                                byte v4 = classbytes[ptr_cur++];
                                byte v5 = classbytes[ptr_cur++];
                                byte v6 = classbytes[ptr_cur++];
                                byte v7 = classbytes[ptr_cur++];
                                t.high_bytes = (u4)(v0 << 24 | v1 << 16 | v2 << 8 | v3);
                                t.low_bytes = (u4)(v4 << 24 | v5 << 16 | v6 << 8 | v7);
                                t.value = BitConverter.ToDouble(new byte[] { v7, v6, v5, v4, v3, v2, v1, v0 }, 0);
                                classfile.constant_pool[i++] = t;
                                large_numeric t2 = new large_numeric();
                                t2.inf = "Double large numeric part";
                                classfile.constant_pool[i] = t2;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_NameAndType:
                            {
                                CONSTANT_NameAndType_info t = new CONSTANT_NameAndType_info();
                                t.tag = tag;
                                t.name_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.descriptor_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;// c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_Utf8:
                            {
                                CONSTANT_Utf8_info t = new CONSTANT_Utf8_info();
                                t.tag = tag;
                                t.length = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.bytes = new u1[t.length];
                                for (int c = 0; c < t.length; c++)
                                    t.bytes[c] = classbytes[ptr_cur++];
                                t.bytes_str = Encoding.Default.GetString(t.bytes);
                                classfile.constant_pool[i] = t;// c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_MethodHandle:
                            {
                                CONSTANT_MethodHandle_info t = new CONSTANT_MethodHandle_info();
                                t.tag = tag;
                                t.reference_kind = (u1)classbytes[ptr_cur++];
                                t.reference_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;//c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_MethodType:
                            {
                                CONSTANT_MethodType_info t = new CONSTANT_MethodType_info();
                                t.tag = tag;
                                t.descriptor_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t;//c_inf;
                            }
                            break;
                        case CONSTANT_type.CONSTANT_InvokeDynamic:
                            {
                                CONSTANT_InvokeDynamic_info t = new CONSTANT_InvokeDynamic_info();
                                t.tag = tag;
                                t.bootstrap_method_attr_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                t.name_and_type_index = (u2)(classbytes[ptr_cur++] << 8 | classbytes[ptr_cur++]);
                                classfile.constant_pool[i] = t; //c_inf;
                            }
                            break;
                        default:
                            {
                                MessageBox.Show("bad CONSTANT parse !");
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("parse_constant_pool() : " + e.Message);
                return false;
            }

            return true;
        }

        #endregion


        enum ACC_flag
        {
            ACC_PUBLIC = 0x0001,
            ACC_FINAL = 0x0010,
            ACC_SUPER = 0x0020,
            ACC_INTERFACE = 0x0200,
            ACC_ABSTRACT = 0x0400,
            ACC_SYNTHETIC = 0x1000,
            ACC_ANNOTATION = 0x2000,
            ACC_ENUM = 0x4000,
            ACC_PRIVATE = 0x0002,
            ACC_PROTECTED = 0x0004,
            ACC_STATIC = 0x0008,
            ACC_VOLATILE = 0x0040,
            ACC_TRANSIENT = 0x0080,
            ACC_SYNCHRONIZED = 0x0020,
            ACC_BRIDGE = 0x0040,
            ACC_VARARGS = 0x0080,
            ACC_NATIVE = 0x0100,
            ACC_STRICT = 0x0800
        }

        enum CONSTANT_type
        {
            CONSTANT_Class = 7,
            CONSTANT_Fieldref = 9,
            CONSTANT_Methodref = 10,
            CONSTANT_InterfaceMethodref = 11,
            CONSTANT_String = 8,
            CONSTANT_Integer = 3,
            CONSTANT_Float = 4,
            CONSTANT_Long = 5,
            CONSTANT_Double = 6,
            CONSTANT_NameAndType = 12,
            CONSTANT_Utf8 = 1,
            CONSTANT_MethodHandle = 15,
            CONSTANT_MethodType = 16,
            CONSTANT_InvokeDynamic = 18
        }

        public struct Class_info //used
        {
            public u4 magic;
            public u2 minor_version;
            public u2 major_version;
            public u2 constant_pool_count;
            public object[] constant_pool;
            public u2 access_flags;
            public u2 this_class;
            public u2 super_class;
            public u2 interfaces_count;
            public u2[] interfaces;
            public u2 fields_count;
            public field_info[] fields;
            public u2 methods_count;
            public method_info[] methods;
            public u2 attributes_count;
            public attribute_info[] attributes;
        }

        public class Frame
        {
            public object[] LocalVariable;
            public List<object> OperandStack = new List<object>();
            public int PC = 0;
            public method_info method;

            public Frame(method_info m)
            {
                method = m;
                LocalVariable = new object[((Code_attribute)method.attributes[0].info).max_locals];
            }

            public object opStackPop()
            {
                object t = OperandStack.Last();
                OperandStack.RemoveAt(OperandStack.Count() - 1);
                return t;
            }

            public void opStackPush(int t)
            {
                OperandStack.Add(t);
            }

            public void opStackPush(long t)
            {
                OperandStack.Add(t);
            }

            public void opStackPush(float t)
            {
                OperandStack.Add(t);
            }

            public void opStackPush(double t)
            {
                OperandStack.Add(t);
            }

            public void opStackPush(object t)
            {
                OperandStack.Add(t);
            }

            public void opStackPush(string t)
            {
                OperandStack.Add(t);
            }

        }

        public struct large_numeric //used
        {
            public string inf;
        }

        public struct CONSTANT_Class_info //used
        {
            public u1 tag;
            public u2 name_index;
        }

        public struct CONSTANT_Fieldref_info //used
        {
            public u1 tag;
            public u2 class_index;
            public u2 name_and_type_index;
        }
        public struct CONSTANT_Methodref_info //used
        {
            public u1 tag;
            public u2 class_index;
            public u2 name_and_type_index;
        }
        public struct CONSTANT_InterfaceMethodref_info //used
        {
            public u1 tag;
            public u2 class_index;
            public u2 name_and_type_index;
        }

        public struct CONSTANT_String_info //used
        {
            public u1 tag;
            public u2 string_index;
        }

        public struct CONSTANT_Integer_info //used
        {
            public u1 tag;
            public i4 bytes;
        }
        public struct CONSTANT_Float_info //used
        {
            public u1 tag;
            public float bytes;
        }
        public struct CONSTANT_Long_info //used
        {
            public u1 tag;
            public u4 high_bytes;
            public u4 low_bytes;
        }
        public struct CONSTANT_Double_info //used
        {
            public u1 tag;
            public u4 high_bytes;
            public u4 low_bytes;
            public double value;
        }

        public struct CONSTANT_NameAndType_info //used
        {
            public u1 tag;
            public u2 name_index;
            public u2 descriptor_index;
        }

        public struct CONSTANT_Utf8_info //used
        {
            public u1 tag;
            public u2 length;
            public u1[] bytes;
            public string bytes_str;
        }

        public struct CONSTANT_MethodHandle_info //used
        {
            public u1 tag;
            public u1 reference_kind;
            public u2 reference_index;
        }

        public struct CONSTANT_MethodType_info //used
        {
            public u1 tag;
            public u2 descriptor_index;
        }

        public struct CONSTANT_InvokeDynamic_info //used
        {
            public u1 tag;
            public u2 bootstrap_method_attr_index;
            public u2 name_and_type_index;
        }



        public struct field_info //used
        {
            public u2 access_flags;
            public u2 name_index;
            public u2 descriptor_index;
            public u2 attributes_count;
            public attribute_info[] attributes;
        }

        public struct method_info //used
        {
            public u2 access_flags;
            public u2 name_index;
            public u2 descriptor_index;
            public u2 attributes_count;
            public attribute_info[] attributes;
        }

        public struct attribute_info //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public object info;
        }


        public struct Exceptions_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 number_of_exceptions;
            public u2[] exception_index_table;
        }


        public struct ConstantValue_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 constantvalue_index;
        }

        public struct exception //used
        {
            public u2 start_pc;
            public u2 end_pc;
            public u2 handler_pc;
            public u2 catch_type;
        }

        public struct Code_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 max_stack;
            public u2 max_locals;
            public u4 code_length;
            public u1[] code;
            public u2 exception_table_length;
            public exception[] exception_table;
            public u2 attributes_count;
            public attribute_info[] attributes;
        }

        public struct line_number //used
        {
            public u2 start_pc;
            public u2 line_numbers;
        }

        public struct LineNumberTable_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 line_number_table_length;
            public line_number[] line_number_table;
        }

        public struct SourceFile_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 sourcefile_index;
        }

        public struct number_of_classes //used
        {
            public u2 inner_class_info_index;
            public u2 outer_class_info_index;
            public u2 inner_name_index;
            public u2 inner_class_access_flags;
        }

        public struct Signature_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 signature_index;
        }

        public struct Synthetic_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
        }

        public struct EnclosingMethod_attribute //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 class_index;
            public u2 method_index;
        }


        public struct InnerClasses_attribute  //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 number_of_classes;
            public number_of_classes[] classes;
        }

        public struct local_variable  //used
        {
            public u2 start_pc;
            public u2 length;
            public u2 name_index;
            public u2 descriptor_index;
            public u2 index;
        }

        public struct LocalVariableTable_attribute  //used
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 local_variable_table_length;
            public local_variable[] local_variable_table;
        }

        public struct local_variable_type
        {
            public u2 start_pc;
            public u2 length;
            public u2 name_index;
            public u2 signature_index;
            public u2 index;
        }

        public struct LocalVariableTypeTable_attribute
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 local_variable_type_table_length;
            public local_variable_type[] local_variable_type_table;
        }

        public struct Deprecated_attribute
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
        }

        public struct SourceDebugExtension_attribute
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u1[] debug_extension;
        }

        public struct bootstrap_method
        {
            public u2 bootstrap_method_ref;
            public u2 num_bootstrap_arguments;
            public u2[] bootstrap_arguments;
        }

        public struct BootstrapMethods_attribute
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u2 num_bootstrap_methods;
            public bootstrap_method[] bootstrap_methods;
        }

        public struct parameter
        {
            public u2 name_index;
            public u2 access_flags;
        }

        public struct MethodParameters_attribute
        {
            public u2 attribute_name_index;
            public u4 attribute_length;
            public u1 parameters_count;
            public parameter[] parameters;
        }
    }
}
