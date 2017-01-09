import sys
import traceback

sys.path.append(r'..\Binaries')

try:

    from PyNet import clr

    print clr
    print

    # Print some stuff
    print 'System.IntPtr:', clr.System.IntPtr
    print 'System.Console:', clr.System.Console
    print 'System.Windows:', clr.System.Windows
    print

    # Load test library
    #clr.Break()
    clr.AddReference('PyNet.Test.exe')

    # Do some stuff
    test = clr.PyNet.Test.SimpleClass()
    test.ThrowException('Plop')

except Exception as e:

    traceback.print_exc()

raw_input()