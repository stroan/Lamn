-------------------------------------------
-- If test 1

function test1(c, d)
  if c then
    local a = "happy days"
    print(a)
  elseif d then
    print "special days"
  else 
    print "unhappy days"
  end
end

test1(true, true)
test1(false, true)
test1(false, false)