import { useState } from "react";

function ListGroup7() {
  let items = ["New York", "San Francisco", "Tokyo", "London", "Paris"];

  // Hook -
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [name, setName] = useState("Tom");

  return (
    <>
      <h1>List for {name}</h1>
      {items.length === 0 && <p>No items found</p>}
      <ul className="list-group">
        {items.map((item, index) => (
          <li
            key={item}
            className={
              selectedIndex === index
                ? "list-group-item active"
                : "list-group-item"
            }
            onClick={() => {
              setSelectedIndex(index);
            }}
          >
            {item}
          </li>
        ))}
      </ul>
    </>
  );
}

export default ListGroup7;
