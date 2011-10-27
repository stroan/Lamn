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

-------------------------------------------
-- If test 2

function test2(c)
  if c then
    return 1,2,3
  end
  return 4,5
end

print(test2(true))
print(test2(false))

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

print(test3(true, true))
print(test3(true, false))