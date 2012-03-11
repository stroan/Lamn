-------------------------------------------
-- If test 1

function test1(c, d)
  if c then
    local a = "happy days"
	return a
  elseif d then
	return "special days"
  else 
	return "unhappy days"
  end
end

assert(test1(true, true) == "happy days")
assert(test1(false, true) == "special days")
assert(test1(false, false) == "unhappy days")

-------------------------------------------
-- If test 2

function test2(c)
  if c then
    return 1,2,3
  end
  return 4,5
end

t1, t2, t3 = test2(true)
assert(t1 == 1)
assert(t2 == 2)
assert(t3 == 3)

t1, t2 = test2(false)
assert(t1 == 4)
assert(t2 == 5)

-------------------------------------------
-- If test 3

function test3(c, d)
  if c then
    if d then
	  return 1
    else
	  return 2
	end
  end
end

assert(test3(true, true) == 1)
assert(test3(true, false) == 2)