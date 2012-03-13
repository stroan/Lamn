print('testing scanner')

-- LAMN DISABLE DUE TO NO DEBUG LIBRARY
--debug = require "debug"


local function dostring (x) return assert(load(x))() end

dostring("x \v\f = \t\r 'a\0a' \v\f\f")
assert(x == 'a\0a' and string.len(x) == 3)

-- escape sequences
assert('\n\"\'\\' == [[

"'\]])

-- LAMN DISABLE DUE TO NO REGEX LIBRARY
--assert(string.find("\a\b\f\n\r\t\v", "^%c%c%c%c%c%c%c$"))

-- assume ASCII just for tests:
assert("\09912" == 'c12')
assert("\99ab" == 'cab')
assert("\099" == '\99')
assert("\099\n" == 'c\10')
assert('\0\0\0alo' == '\0' .. '\0\0' .. 'alo')

--assert(010 .. 020 .. -030 == "1020-30")