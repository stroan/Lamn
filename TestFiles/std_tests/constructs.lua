;;print "testing syntax";;

-- local debug = require "debug"

-- testing semicollons
do ;;; end
; do ; a = 3; assert(a == 3) end;
;


-- testing priorities

assert(2^3^2 == 2^(3^2));
assert(2^3*4 == (2^3)*4);
assert(2^-2 == 1/4 and -2^- -2 == - - -4);