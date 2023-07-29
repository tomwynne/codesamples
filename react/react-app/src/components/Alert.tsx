import React, { ReactNode } from "react";

interface Props {
  //children: string; // text only, not html
  children: ReactNode; // inner html code
  onClose: () => void;
}

export const Alert = ({ children, onClose }: Props) => {
  return (
    <div className="alert alert-primary alert-dismissible" role="alert">
      {children}
      <button
        type="button"
        className="btn-close"
        data-bs-dismiss="alert"
        aria-label="Close"
        onClick={onClose}
      ></button>
    </div>
  );
};
